using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            ProductsCategoriesViewModel viewModel = new ProductsCategoriesViewModel();

            viewModel.Categories =
                _unitOfWork?.Category?.GetAll().ToList();

            viewModel.Products =
                _unitOfWork?.Product?.GetAll(includeProperties: "Category").ToList();

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

        public IActionResult Filter(bool isRecomended, int max, int min, int categoryId)
        {
            ProductsCategoryViewModel viewModel = new ProductsCategoryViewModel();

            viewModel.Products = _unitOfWork.Product.GetAll(p => p.CategoryId == categoryId, includeProperties: "Category").ToList();

            viewModel.Products = this.FilterByRecomended(categoryId, isRecomended, viewModel.Products);
            viewModel.Products = this.FilterByPrice(categoryId, min, max, viewModel.Products);

            viewModel.Category =
                _unitOfWork?.Category?.Get(c => c.Id == categoryId);

            ViewBag.IsRecomended = isRecomended;
            ViewBag.Max = max;
            ViewBag.Min = min;

            return View("Category", viewModel);
        }
        private List<Product> FilterByRecomended(int categoryId, bool isRecomended, List<Product> products)
        {
            if (isRecomended)
            {
                return products.FindAll(p => p.IsRecomented == isRecomended);
            }
            return products;
        }
        private List<Product> FilterByPrice(int categoryId, int min, int max, List<Product> products)
        {
            return products.FindAll(p => p.ListPrice > min && p.ListPrice < max);
        }


        public IActionResult Details(int? productId)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unitOfWork?.Product?
                    .Get(p => p.Id == productId, includeProperties: "Category"),
                Count = 1,
                ProductId = productId
            };


            return View(shoppingCart);
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