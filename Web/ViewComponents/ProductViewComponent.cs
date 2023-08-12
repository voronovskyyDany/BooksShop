using Microsoft.AspNetCore.Mvc;
using Models;

namespace Web.ViewComponents
{
    public class ProductViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Product product)
        {
            return View(product);
        }
    }
}
