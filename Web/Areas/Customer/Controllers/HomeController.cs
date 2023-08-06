using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;
using System.Security.Claims;
using Utility;

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
            IEnumerable<Product>? products = _unitOfWork?.Product?.GetAll(includeProperties: "Category");
            return View(products);
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