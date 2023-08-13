using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace Web.ViewComponents
{
    public class CompareProductViewComponent : ViewComponent
    {
        private IUnitOfWork _unitOfWork;

        public CompareProductViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IViewComponentResult Invoke(Product product) {

            List<Comment> comments = _unitOfWork.Comment.GetAll(c => c.ProductId == product.Id).ToList();

            if(comments.Count != 0)
            {
                int sumRate = comments.Sum(c => c.Rate);
                ViewBag.Rate = sumRate / comments.Count;
            }
           

            return View(product);
        }
    }
}
