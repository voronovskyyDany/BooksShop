using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels;
using Stripe.Checkout;
using System.Security.Claims;
using Utility;

namespace Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public ShoppingCartViewModel? ShoppingCartViewModel { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public IActionResult Index()
        {

            var claimsIndentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIndentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ShoppingCartViewModel = new()
            {
                ShoppingCartList = _unitOfWork?.ShoppingCart?
                                    .GetAll(u => u.ApplicationUserId == userId,
                                    includeProperties: "Product"
                                    ),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }



            return View(ShoppingCartViewModel);
        }



        public IActionResult Summary()
        {
            var claimsIndentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIndentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ShoppingCartViewModel = new()
            {
                ShoppingCartList = _unitOfWork?.ShoppingCart?
                                    .GetAll(u => u.ApplicationUserId == userId,
                                    includeProperties: "Product"
                                    ),
                OrderHeader = new()
            };


            ShoppingCartViewModel.OrderHeader.ApplicationUser =
                                        _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartViewModel.OrderHeader.Name = ShoppingCartViewModel.OrderHeader.ApplicationUser.Name;
            ShoppingCartViewModel.OrderHeader.PhoneNumber = ShoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartViewModel.OrderHeader.StreetAddress = ShoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartViewModel.OrderHeader.City = ShoppingCartViewModel.OrderHeader.ApplicationUser.City;
            ShoppingCartViewModel.OrderHeader.State = ShoppingCartViewModel.OrderHeader.ApplicationUser.State;
            ShoppingCartViewModel.OrderHeader.PostalCode = ShoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;


            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartViewModel);
        }


        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var claimsIndentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIndentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ShoppingCartViewModel.ShoppingCartList = _unitOfWork?.ShoppingCart?
                        .GetAll(u => u.ApplicationUserId == userId,
                                includeProperties: "Product"
                            );

            ShoppingCartViewModel.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartViewModel.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser =
                                        _unitOfWork.ApplicationUser.Get(u => u.Id == userId);



            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }


            if (applicationUser?.CompanyId.GetValueOrDefault() == 0)
            {
                // it's a regular customer
                ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                // it's a company user
                ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusApproved;
            }


            _unitOfWork?.OrderHeader?.Add(ShoppingCartViewModel.OrderHeader);
            _unitOfWork?.Save();

            foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartViewModel.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork?.OrderDetail?.Add(orderDetail);
                _unitOfWork?.Save();
            }



            if (applicationUser?.CompanyId.GetValueOrDefault() == 0)
            {
                // it's a regular customer account and we need to capture payment
                // stripe or another payment logic...

                var domain = "https://localhost:7173";

                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"/customer/cart/OrderConfirmation?id={ShoppingCartViewModel.OrderHeader.Id}",
                    CancelUrl = domain + $"/customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),             
                    Mode = "payment",
                };


                foreach (var item in ShoppingCartViewModel.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long?)(item.Price * 100), // $20.50 => 2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item?.Product?.Title?.ToString(),
                            }
                        },
                        Quantity = item?.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);

                _unitOfWork?.OrderHeader?.UpdateStripePaymentID(ShoppingCartViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork?.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }




            return RedirectToAction(
                nameof(OrderConfirmation),
                new { ShoppingCartViewModel?.OrderHeader.Id });
        }




        public IActionResult OrderConfirmation(int id)
        {

            OrderHeader? orderHeader = _unitOfWork?.OrderHeader?.Get(
                                o => o.Id  == id,
                                includeProperties: "ApplicationUser"); 
            if(orderHeader?.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // this is an order by customer
                var service = new SessionService();
                Session session = service.Get(orderHeader?.SessionId);

                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork?.OrderHeader?.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork?.OrderHeader?.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork?.Save();
                }
                HttpContext.Session.Clear();

            }


            List<ShoppingCart>? shoppingCarts = _unitOfWork?.ShoppingCart?
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();


            _unitOfWork?.ShoppingCart?.RemoveRange(shoppingCarts);
            _unitOfWork?.Save();


            return View(id);
        }



        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork?.ShoppingCart?.Get(c => c.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork?.ShoppingCart?.Update(cartFromDb);
            _unitOfWork?.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork?.ShoppingCart?.Get(c => c.Id == cartId);
            if (cartFromDb.Count <= 1)
            {
                // remove that from cart
                _unitOfWork?.ShoppingCart?.Remove(cartFromDb);
                int count = (int)_unitOfWork?.ShoppingCart?
                        .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).ToList().Count - 1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork?.ShoppingCart?.Update(cartFromDb);
            }

            _unitOfWork?.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork?.ShoppingCart?.Get(c => c.Id == cartId);
            _unitOfWork?.ShoppingCart?.Remove(cartFromDb);
            _unitOfWork?.Save();
            int count = (int)_unitOfWork?.ShoppingCart?
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.SessionCart, count);
            return RedirectToAction(nameof(Index));
        }

        private double? GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
                return shoppingCart?.Product?.Price;
            else
            {
                if (shoppingCart.Count <= 1000)
                {
                    return shoppingCart.Product?.Price50;
                }
                else
                {
                    return shoppingCart?.Product?.Price100;
                }
            }
        }
    }
}