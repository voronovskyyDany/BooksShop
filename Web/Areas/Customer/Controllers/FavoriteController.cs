using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;
using System.Text;
using Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class FavoriteController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public FavoriteController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<int> comparisonIds = JsonConvert.DeserializeObject<List<int>>(this.Get());

            List<Product> products = new();
            if (comparisonIds != null)
            {
                foreach (var id in comparisonIds)
                {
                    products.Add(_unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category"));
                }
            }
            return View("Favorite", products);
        }

        [HttpGet]
        public string Get()
        {
            List<int> favorites = HttpContext.Session.Get<List<int>>(SD.SessionFavorite);
            return JsonConvert.SerializeObject(favorites);
        }
        
        [HttpPost]
        public string Add(int productId)
        {
            List<int> favorites = HttpContext.Session.Get<List<int>>(SD.SessionFavorite);
            if (favorites == null)
            {
                favorites = new List<int>();
                favorites.Add(productId);
            }
            else
            {
                if (!favorites.Contains(productId))
                {
                    favorites.Add(productId);
                }
            }
            HttpContext.Session.Set(SD.SessionFavorite, favorites);

            return JsonConvert.SerializeObject(favorites);
        }

        [HttpDelete]
        public string Remove(int productId)
        {
            List<int> favorites = HttpContext.Session.Get<List<int>>(SD.SessionFavorite);

            if(favorites!= null)
            {
                favorites.Remove(productId);
                HttpContext.Session.Set(SD.SessionFavorite, favorites);
            }
            return JsonConvert.SerializeObject(favorites);
        }
    }
}
