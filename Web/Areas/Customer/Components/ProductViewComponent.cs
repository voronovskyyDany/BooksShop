using Microsoft.AspNetCore.Mvc;
using Models;

namespace Web.Areas.Customer.Components
{
    public class ProductViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Product product)
        {
            return View(product);
        }
    }
}
