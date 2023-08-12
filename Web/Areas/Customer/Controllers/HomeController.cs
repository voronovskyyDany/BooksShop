using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Models;
using Models.ViewModels;
using Stripe;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using Utility;
using Product = Models.Product;

namespace Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            HomeViewModel viewModel = new HomeViewModel();

            viewModel.Categories =
                _unitOfWork?.Category?.GetAll().ToList();

            viewModel.Products =
                _unitOfWork?.Product?.GetAll(includeProperties: "Category").ToList();

            viewModel.Recomended =
                _unitOfWork?.Product?.GetAll(p => p.IsRecomented == true, includeProperties: "Category").ToList();

            viewModel.History = this.getHisory();

            return View(viewModel);
        }

        public IActionResult Category(int? id)
        {
            ProductsCategoryViewModel viewModel = new ProductsCategoryViewModel();

            viewModel.Products =
                _unitOfWork?.Product?.GetAll(p => p.CategoryId == id, includeProperties: "Category").ToList();

            viewModel.Category = 
                _unitOfWork?.Category?.Get(c => c.Id == id);

            ViewBag.IsRecomended = false;
            ViewBag.Min = 10;
            ViewBag.Max = 1000;

            return View(viewModel);
        }

        public IActionResult Search(string text, int categoryId)
        {
            if (text == null)
                text = "";

            if(categoryId == 0)
            {
                ProductsCategoriesViewModel viewModel = new ProductsCategoriesViewModel();

                viewModel.Products =
                    _unitOfWork?.Product?.GetAll(p => p.Title.Contains(text), includeProperties: "Category").ToList();

                viewModel.Categories =
                    _unitOfWork?.Category?.GetAll().ToList();

                return View("Index", viewModel);
            }

            else
            {
                ProductsCategoryViewModel viewModel = new ProductsCategoryViewModel();

                viewModel.Products =
                       _unitOfWork?.Product?.GetAll(p => p.Title.Contains(text) && p.CategoryId == categoryId, includeProperties: "Category").ToList();

                viewModel.Category =
                    _unitOfWork?.Category?.Get(c => c.Id == categoryId);

                ViewBag.IsRecomended = false;
                ViewBag.Min = 10;
                ViewBag.Max = 1000;

                return View("Category", viewModel);
            }


        }

        public IActionResult Filter(bool isRecomended, int max, int min, string author, int categoryId)
        {
            ProductsCategoryViewModel viewModel = new ProductsCategoryViewModel();

            List<Product> products = _unitOfWork.Product.GetAll(p => p.CategoryId == categoryId, includeProperties: "Category").ToList();
            
            this.FilterByRecomended(categoryId, isRecomended, products);
            this.FilterByPrice(categoryId, min, max, products);
            this.FilterByAuthor(categoryId, author, products);

            viewModel.Products = products;

            viewModel.Category =
                _unitOfWork?.Category?.Get(c => c.Id == categoryId);

            ViewBag.IsRecomended = isRecomended;
            ViewBag.Max = max;
            ViewBag.Min = min;
            ViewBag.Author = author;

            return View("Category", viewModel);
        }
        private void FilterByRecomended(int categoryId, bool isRecomended, List<Product> products)
        {
            if (isRecomended)
            {
                products.RemoveAll(p => p.IsRecomented != isRecomended);
            }
        }
        private void FilterByPrice(int categoryId, int min, int max, List<Product> products)
        {
            products.RemoveAll(p => p.Price < min || p.Price > max);
        }
        private void FilterByAuthor(int cateogryId, string author, List<Product> products)
        {
            if(author == null)
                author = string.Empty;
            products.RemoveAll(p => !p.Author.Contains(author));
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unitOfWork?.Product?
                    .Get(p => p.Id == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId
            };
            this.addToHistory  (productId);


            ViewBag.Comments = _unitOfWork.Comment.GetAll(c => c.ProductId == productId, includeProperties: "Product,ApplicationUser").ToList(); 

            return View(shoppingCart);
        }
        private void addToHistory(int productId)
        {
            List<int> historyIds = HttpContext.Session.Get<List<int>>(SD.SessionHistory);
            if(historyIds == null)
            {
                historyIds = new();
                historyIds.Add(productId);
            }
            else
            {
                if(!historyIds.Contains(productId)) 
                {
                    historyIds.Add(productId);
                }
            }
            HttpContext.Session.Set(SD.SessionHistory, historyIds);
        }
        private List<Product> getHisory()
        {
            List <Product> hisory = new();
            List<int> historyIds = HttpContext.Session.Get<List<int>>(SD.SessionHistory);
            if(historyIds != null)
            {
                foreach (var id in historyIds)
                {
                    hisory.Add(_unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category"));
                }
            }
            return hisory;
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {

            var claimsIndentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIndentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            shoppingCart.ApplicationUserId = userId;
          
          
            ShoppingCart? cartFromDb = _unitOfWork?.ShoppingCart?
                    .Get(u => u.ApplicationUserId == userId &&
                        u.ProductId == shoppingCart.ProductId);

            if(cartFromDb != null)
            {
                // shopping cart exists
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork?.ShoppingCart?.Update(cartFromDb);
                _unitOfWork?.Save();

            }
            else
            {
                // add cart record
                _unitOfWork?.ShoppingCart?.Add(shoppingCart);
            _unitOfWork?.Save();
            HttpContext.Session.SetInt32(SD.SessionCart, (int)_unitOfWork?.ShoppingCart?
                .GetAll(u => u.ApplicationUserId == userId).Count());
            }


            TempData["Success"] = "Cart Updated Successfully";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public IActionResult SendComment (Comment comment)
        {
            var claimsIndentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIndentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            comment.ApplicationUserId = userId;
            comment.Created = DateTime.Now;

            _unitOfWork.Comment.Add(comment);
            _unitOfWork.Save();

            comment.ApplicationUserId = userId;
            return RedirectToAction(nameof(Details), routeValues: new { productId = comment.ProductId });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}