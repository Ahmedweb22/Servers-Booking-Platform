using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using Shatbly.Reports;

namespace Shatbly.Areas.Admin.Controllers
{
    [Area(SD.ADMIN_AREA)]
    [Authorize(Roles = $"{SD.ROLE_SUPER_ADMIN},{SD.ROLE_ADMIN}")]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<UserController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        public UserController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
            IStringLocalizer<UserController> localizer, IStringLocalizer<SharedResource> sharedLocalizer)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
        }
        public async Task<IActionResult> Index(string? name, int page = 1)
        {
            var users = _userManager.Users.AsNoTracking();

            if (name is not null)
                users = users.Where(e => e.UserName.Contains(name));

            if (page < 1)
                page = 1;
            int pageSize = 10;
            int currentPage = page;
            double totalCount = Math.Ceiling(users.Count() / (double)pageSize);
            users = users.Skip((page - 1) * pageSize).Take(pageSize);
            var usersList = await users.ToListAsync();
            var model = new List<UserWithRoleVM>();
            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserWithRoleVM
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    RoleName = roles.FirstOrDefault()
                });
            }




            return View(new UsersVM
            {
                Users = model,
                TotalPages = totalCount,
                CurrentPage = currentPage
            });
        }
        [HttpGet]
        public IActionResult Create()
        {

            var roles = _roleManager.Roles.AsNoTracking().AsQueryable();

            return View(new CreateUserVM
            {
                Roles = roles.ToList()
            });
        }
        //[HttpPost]
        //public async Task<IActionResult> Create(CreateUserVM createUserVM)
        //{
        //    ModelState.Remove("Id");
        //    ModelState.Remove("User");
        //    ModelState.Remove("Roles");
        //    if (!ModelState.IsValid)
        //    {
        //        TempData["error-notification"] = "Invalid Data";
        //        createUserVM.Roles = _roleManager.Roles.AsNoTracking().ToList();

        //        return View(createUserVM);
        //    }
        //    var user = new User
        //    {
        //        FName = createUserVM.FName,
        //        LName = createUserVM.LName,
        //        UserName = createUserVM.UserName,
        //        Email = createUserVM.Email,
        //        Phone = createUserVM.Phone
        //    };
        //    var result = await _userManager.CreateAsync(user, createUserVM.Password);
        //    if (!result.Succeeded)
        //    {
        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError(string.Empty, error.Description);
        //        }
        //        TempData["error-notification"] = $"Save Failed";
        //    }
        //    else
        //    {
        //        await _userManager.AddToRoleAsync(user, createUserVM.RoleName);
        //        await _userManager.UpdateAsync(user);
        //       TempData["success-notification"] = $"Save Successful";
        //    }


        //    return RedirectToAction(nameof(Index));
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM createUserVM)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Roles");

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["InvalidData"].Value;
                createUserVM.Roles = _roleManager.Roles.AsNoTracking().ToList();
                return View(createUserVM);
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(createUserVM.Email);
            if (existingUserByEmail != null)
            {
                ModelState.AddModelError("Email", _localizer["EmailAlreadyExists"].Value);
                createUserVM.Roles = _roleManager.Roles.AsNoTracking().ToList();
                return View(createUserVM);
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(createUserVM.UserName);
            if (existingUserByUsername != null)
            {
                ModelState.AddModelError("UserName", _localizer["UsernameAlreadyExists"].Value);
                createUserVM.Roles = _roleManager.Roles.AsNoTracking().ToList();
                return View(createUserVM);
            }

            var existingUserByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == createUserVM.Phone);
            if (existingUserByPhone != null)
            {
                ModelState.AddModelError("Phone", _localizer["PhoneAlreadyExists"].Value);
                createUserVM.Roles = _roleManager.Roles.AsNoTracking().ToList();
                return View(createUserVM);
            }

            var user = new User
            {
                FName = createUserVM.FName,
                LName = createUserVM.LName,
                Name = createUserVM.FName + " " + createUserVM.LName,
                UserName = createUserVM.UserName,
                Email = createUserVM.Email,
                Phone = createUserVM.Phone
            };

            var result = await _userManager.CreateAsync(user, createUserVM.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                TempData["error-notification"] = _localizer["SaveFailed"].Value;
                createUserVM.Roles = _roleManager.Roles.AsNoTracking().ToList();
                return View(createUserVM);
            }

            if (!string.IsNullOrEmpty(createUserVM.RoleName))
            {
                await _userManager.AddToRoleAsync(user, createUserVM.RoleName);
            }

            TempData["success-notification"] = _localizer["SaveSuccessful"].Value;
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var roles = _roleManager.Roles.AsNoTracking().AsQueryable();
            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserVM
            {
                Id = user.Id,
                FName = user.FName,
                LName = user.LName,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = userRoles.FirstOrDefault(),
                Roles = roles.AsEnumerable()
            };

            if (model.RoleName != SD.ROLE_WORKER && TempData.TryGetValue("SavedPassword", out var savedPassword))
            {
                model.Password = savedPassword as string;
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVM editUserVM)
        {
            ModelState.Remove("Id");
            ModelState.Remove("User");
            ModelState.Remove("Roles");
            if (string.IsNullOrEmpty(editUserVM.Password))
            {
                ModelState.Remove("Password");
            }

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["InvalidData"].Value;
                var roles = _roleManager.Roles.AsNoTracking().AsQueryable();
                editUserVM.Roles = roles.AsEnumerable();
                return View(editUserVM);
            }

            var user = await _userManager.FindByIdAsync(editUserVM.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Duplicate checks
            var existingUserByEmail = await _userManager.FindByEmailAsync(editUserVM.Email);
            if (existingUserByEmail != null && existingUserByEmail.Id != editUserVM.Id)
            {
                ModelState.AddModelError("Email", _localizer["EmailAlreadyExists"].Value);
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(editUserVM.UserName);
            if (existingUserByUsername != null && existingUserByUsername.Id != editUserVM.Id)
            {
                ModelState.AddModelError("UserName", _localizer["UsernameAlreadyExists"].Value);
            }

            var existingUserByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == editUserVM.Phone);
            if (existingUserByPhone != null && existingUserByPhone.Id != editUserVM.Id)
            {
                ModelState.AddModelError("Phone", _localizer["PhoneAlreadyExists"].Value);
            }

            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = _sharedLocalizer["InvalidData"].Value;
                var roles = _roleManager.Roles.AsNoTracking().AsQueryable();
                editUserVM.Roles = roles.AsEnumerable();
                return View(editUserVM);
            }

            user.FName = editUserVM.FName;
            user.LName = editUserVM.LName;
            user.Name = editUserVM.FName + " " + editUserVM.LName;
            user.UserName = editUserVM.UserName;
            user.Email = editUserVM.Email;
            user.Phone = editUserVM.Phone;
            user.PhoneNumber = editUserVM.Phone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["error-notification"] = _localizer["UpdateFailed"].Value;
                var roles = _roleManager.Roles.AsNoTracking().AsQueryable();
                editUserVM.Roles = roles.AsEnumerable();
                return View(editUserVM);
            }

            // Update Password if provided
            if (!string.IsNullOrEmpty(editUserVM.Password))
            {
                var isUserWorker = await _userManager.IsInRoleAsync(user, SD.ROLE_WORKER) || editUserVM.RoleName == SD.ROLE_WORKER;
                if (isUserWorker)
                {
                    ModelState.AddModelError("Password", "Admin cannot change the password for workers.");
                    TempData["error-notification"] = "Admin cannot change the password for workers.";
                    var roles = _roleManager.Roles.AsNoTracking().AsQueryable();
                    editUserVM.Roles = roles.AsEnumerable();
                    return View(editUserVM);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, editUserVM.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    TempData["error-notification"] = _localizer["UpdateFailed"].Value;
                    var roles = _roleManager.Roles.AsNoTracking().AsQueryable();
                    editUserVM.Roles = roles.AsEnumerable();
                    return View(editUserVM);
                }
                TempData["SavedPassword"] = editUserVM.Password;
            }

            // Update Roles
            var userRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, userRoles);
            await _userManager.AddToRoleAsync(user, editUserVM.RoleName);

            TempData["success-notification"] = _localizer["UpdateSuccessful"].Value;
            return RedirectToAction(nameof(Edit), new { id = editUserVM.Id });
        }
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["error-notification"] = _localizer["DeleteFailed"].Value;
            }
            else
            {
                TempData["success-notification"] = _localizer["DeleteSuccessful"].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportPdf([FromServices] Shatbly.DataAccess.ApplicationDbContext context)
        {
            var users = await _userManager.Users
                .Include(u => u.Orders)
                .Include(u => u.ClientBookings)
                .ToListAsync();

            var roles = await context.Roles.ToListAsync();
            var userRoles = await context.UserRoles.ToListAsync();
            var roleNames = roles.ToDictionary(r => r.Id, r => r.Name ?? "Unknown");
            var userRoleMap = userRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => roleNames.GetValueOrDefault(g.First().RoleId, "Customer"));

            var report = new SimpleReport(users, userRoleMap);
            var pdfBytes = report.GeneratePdf();

            return File(pdfBytes, "application/pdf", "UsersReport.pdf");
        }
    }
}
