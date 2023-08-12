using Microsoft.AspNetCore.Mvc;
using Models;

namespace Web.ViewComponents
{
    public class CommentViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Comment comment)
        {
            return View(comment);
        }
    }
}
