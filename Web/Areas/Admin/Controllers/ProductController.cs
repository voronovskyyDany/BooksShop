using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Models.ViewModels;
using System.Data;
using Utility;

namespace Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product>? products = _unitOfWork.Product?.GetAll(includeProperties: "Category").ToList();
            return View(products);
        }

        public IActionResult Upsert(int? id)
        {

            ProductViewModel productViewModel = new()
            {
                Product = new Product(),
                Categories = _unitOfWork?.Category?
                        .GetAll().Select(c => new SelectListItem
                        {
                            Text = c.Name,
                            Value = c.Id.ToString(),
                        })
            };

            if(id == null || id == 0)
            {
                // Create
                return View(productViewModel);
            }
            else
            {
                // Update
                productViewModel.Product = _unitOfWork?.Product?
                    .Get(p => p.Id == id);
                return View(productViewModel);
            }

        }


        [HttpPost]
        public IActionResult Upsert(ProductViewModel productViewModel, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string? wwwRootPath = _webHostEnvironment.WebRootPath;
                if(file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string imageFolderPath = Path.Combine(wwwRootPath, @"images\product");
                    string imagePath = Path.Combine(imageFolderPath, fileName);


                    if(!string.IsNullOrEmpty(productViewModel?.Product?.ImageUrl))
                    {
                        // delete the old image
                        var oldImagePath = 
                            Path.Combine(wwwRootPath, productViewModel.Product.ImageUrl.TrimStart('\\'));

                        if(System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }

                    }

                    using var fileStream = new FileStream(imagePath, FileMode.Create);
                    file.CopyTo(fileStream);

                    productViewModel.Product.ImageUrl = @"\images\product\" + fileName;

                }

                if(productViewModel.Product.Id == 0)
                {
                    _unitOfWork.Product?.Add(productViewModel.Product);
                }
                else
                {
                    _unitOfWork.Product?.Update(productViewModel.Product);
                }

                _unitOfWork.Save();
                TempData["Success"] = "Product created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productViewModel.Categories = _unitOfWork?.Category?
                        .GetAll().Select(c => new SelectListItem
                        {
                            Text = c.Name,
                            Value = c.Id.ToString(),
                        });

                return View(productViewModel);
            }
        }




        #region

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product>? products = _unitOfWork.Product?.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = products });
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product? product = _unitOfWork.Product?.Get(p => p.Id == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }


            string? wwwRootPath = _webHostEnvironment.WebRootPath;
            var oldImagePath =
                            Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }



            _unitOfWork.Product?.Remove(product);
            _unitOfWork.Save();


            return Json(new { success = true, message = "Delete Successful" });

        }


        #endregion




    }

}



