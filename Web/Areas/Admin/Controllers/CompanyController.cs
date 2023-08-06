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
    public class CompanyController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company>? Companies = _unitOfWork.Company?.GetAll().ToList();
            return View(Companies);
        }

        public IActionResult Upsert(int? id)
        {

            if(id == null || id == 0)
            {
                // Create
                return View(new Company());
            }
            else
            {
                // Update
                Company? company = _unitOfWork?.Company?
                    .Get(p => p.Id == id);
                return View(company);
            }

        }


        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                

                if(company.Id == 0)
                {
                    _unitOfWork.Company?.Add(company);
                }
                else
                {
                    _unitOfWork.Company?.Update(company);
                }

                _unitOfWork.Save();
                TempData["Success"] = "Company created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return View(company);
            }
        }


        #region

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company>? Companies = _unitOfWork.Company?.GetAll().ToList();
            return Json(new { data = Companies });
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company? Company = _unitOfWork.Company?.Get(p => p.Id == id);
            if (Company == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company?.Remove(Company);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });

        }

        #endregion

    }

}



