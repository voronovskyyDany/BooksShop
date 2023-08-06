using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.ViewModels;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;
using Utility;

namespace Web.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize]
    public class OrderController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderViewModel OrderViewModel { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;
        }


        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Details(int id)
        {

            OrderViewModel = new()
            {
                OrderHeader = _unitOfWork?.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork?.OrderDetail.GetAll(o => o.OrderHeaderId == id, includeProperties: "Product")
            };

            return View(OrderViewModel);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {

            var orderHeaderFromDb = _unitOfWork?.OrderHeader?.Get(o => o.Id == OrderViewModel.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderViewModel.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderViewModel.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderViewModel.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderViewModel.OrderHeader.City;
            orderHeaderFromDb.State = OrderViewModel.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderViewModel.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            }

            _unitOfWork?.OrderHeader?.Update(orderHeaderFromDb);
            _unitOfWork?.Save();

            TempData["Success"] = "Order Details Updated Successfully";


            return RedirectToAction(nameof(Details), new { id = orderHeaderFromDb.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork?.OrderHeader?.UpdateStatus(OrderViewModel.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork?.Save();

            TempData["Success"] = "Order Details Updated Successfully";
            return RedirectToAction(nameof(Details), new { id = OrderViewModel.OrderHeader.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {

            var orderHeaderFromDb = _unitOfWork?.OrderHeader?.Get(o => o.Id == OrderViewModel.OrderHeader.Id);
            orderHeaderFromDb.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;
            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork?.OrderHeader?.Update(orderHeaderFromDb);
            _unitOfWork?.Save();

            TempData["Success"] = "Order Shipped Successfully";
            return RedirectToAction(nameof(Details), new { id = OrderViewModel.OrderHeader.Id });
        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDb = _unitOfWork?.OrderHeader?.Get(o => o.Id == OrderViewModel.OrderHeader.Id);
            if (orderHeaderFromDb.OrderStatus == SD.PaymentStatusApproved)
            {
                // refund
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork?.OrderHeader?.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork?.OrderHeader?.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork?.Save();
            TempData["Success"] = "Order Cancelled Successfully";
            return RedirectToAction(nameof(Details), new { id = OrderViewModel.OrderHeader.Id });
        }


        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {

            OrderViewModel.OrderHeader = _unitOfWork?.OrderHeader
                    .Get(o => o.Id == OrderViewModel.OrderHeader.Id,
                                includeProperties: "ApplicationUser");
            OrderViewModel.OrderDetail = _unitOfWork?.OrderDetail
                    .GetAll(o => o.OrderHeaderId == OrderViewModel.OrderHeader.Id,
                                includeProperties: "Product");


            // stripe logic...
            var domain = "https://localhost:7173";

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"/admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeader.Id}",
                CancelUrl = domain + $"/admin/order/details?id={OrderViewModel.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderViewModel.OrderDetail)
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

            _unitOfWork?.OrderHeader?.UpdateStripePaymentID(OrderViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork?.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }



        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader? orderHeader = _unitOfWork?.OrderHeader?.Get(
                                o => o.Id == orderHeaderId);
            if (orderHeader?.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // this is an order by company
                var service = new SessionService();
                Session session = service.Get(orderHeader?.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork?.OrderHeader?.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork?.OrderHeader?.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork?.Save();
                }

            }


           
            _unitOfWork?.Save();


            return View(orderHeaderId);
        }





        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader>? orderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork?.OrderHeader?
                    .GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                orderHeaders = _unitOfWork?.OrderHeader?
                    .GetAll(
                        o => o.ApplicationUserId == userId,
                        includeProperties: "ApplicationUser").ToList();
            };

            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders?.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders?.Where(o => o.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders?.Where(o => o.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders?.Where(o => o.OrderStatus == SD.StatusApproved);
                    break;

            }



            return Json(new { data = orderHeaders });
        }

        #endregion

    }
}
