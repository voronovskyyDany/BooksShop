using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Data;
using Utility;

namespace Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }



        public IActionResult RoleManagement(string userId)
        {
            
            RoleManagementViewModel roleManagementViewModel = new()
            {
                ApplicationUser = _unitOfWork?.ApplicationUser?.Get(u => u.Id == userId, includeProperties: "Company"),
                RoleList = _roleManager.Roles.
                        Select(x => x.Name).Select(i => new SelectListItem
                        {
                            Text = i,
                            Value = i
                        }),
                CompanyList = _unitOfWork.Company.GetAll()
                        .Select(i => new SelectListItem
                        {
                            Text = i.Name,
                            Value = i.Id.ToString()
                        })
            };


            roleManagementViewModel.ApplicationUser.Role =
                            _userManager.GetRolesAsync(_unitOfWork?.ApplicationUser?
                            .Get(u => u.Id == userId)).GetAwaiter().GetResult()
                            .FirstOrDefault();
            return View(roleManagementViewModel);
        }


        [HttpPost]
        public IActionResult RoleManagement(RoleManagementViewModel roleManagementViewModel)
        {

            string oldRole = _userManager.GetRolesAsync(_unitOfWork?.ApplicationUser?
                            .Get(u => u.Id == roleManagementViewModel.ApplicationUser.Id))
                            .GetAwaiter().GetResult().FirstOrDefault();


            ApplicationUser applicationUser =
                    _unitOfWork?.ApplicationUser.Get(u => u.Id == roleManagementViewModel.ApplicationUser.Id);

            if (!(roleManagementViewModel.ApplicationUser.Role == oldRole))
            {
                // a role was updated
               

                if(roleManagementViewModel.ApplicationUser.Role == SD.Role_Company) {
                    applicationUser.CompanyId = roleManagementViewModel.ApplicationUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;

                }

                _unitOfWork?.ApplicationUser.Update(applicationUser);
                _unitOfWork?.Save();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagementViewModel.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if(oldRole == SD.Role_Company &&
                    applicationUser?.CompanyId != roleManagementViewModel.ApplicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleManagementViewModel.ApplicationUser.CompanyId;
                    _unitOfWork?.ApplicationUser.Update(applicationUser);
                    _unitOfWork?.Save();
                }
            }

            return RedirectToAction(nameof(Index));
        }



        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser>? users = _unitOfWork.
                ApplicationUser?.GetAll(includeProperties: "Company").ToList();

            foreach (var user in users)
            {
                user.Role = _userManager.GetRolesAsync(user)
                                .GetAwaiter().GetResult().FirstOrDefault();

                if (user.Company == null)
                {
                    user.Company = new()
                    {
                        Name = "",
                    };
                }
            }

            return Json(new { data = users });
        }


        [HttpPost]
        public IActionResult LockUnlock([FromBody] string? id)
        {

            var user = _unitOfWork?.ApplicationUser?.Get(u => u.Id == id);

            if (user == null)
            {
                return Json(new { success = true, message = "Error while Locking/Unlocking" });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                // user is currently locked and we need to unlock them
                user.LockoutEnd = DateTime.Now;
            }
            else
            {
                user.LockoutEnd = DateTime.Now.AddYears(1000);
            }

            _unitOfWork?.ApplicationUser?.Update(user);
            _unitOfWork?.Save();

            return Json(new { success = true, message = "Operation Successful" });
        }

        #endregion

    }

}



