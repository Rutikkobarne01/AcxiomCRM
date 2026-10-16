using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(term)));
            }

            var users = await usersQuery.OrderByDescending(u => u.CreatedDate).ToListAsync();
            var userViewModels = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var primaryRole = userRoles.FirstOrDefault() ?? "None";

                if (!string.IsNullOrEmpty(role) && primaryRole != role)
                {
                    continue;
                }

                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);

                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Role = primaryRole,
                    IsActive = user.IsActive,
                    AccessFailedCount = failedAttempts,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = lockoutEnd,
                    CreatedDate = user.CreatedDate
                });
            }

            var model = new UserListViewModel
            {
                Users = userViewModels,
                SearchTerm = search,
                RoleFilter = role
            };

            return View(model);
        }

        // GET: Users/Create (Admin only)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "User with this email already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                await _auditService.LogAsync("Create", "User", user.Id, null, $"User: {user.Email}, Role: {model.Role}", "Administrator created new user");

                TempData["SuccessMessage"] = $"User '{user.FullName}' ({model.Role}) created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: Users/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive
            };

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.IsActive = model.IsActive;

            // Optional password reset
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError("NewPassword", error.Description);
                    }
                    return View(model);
                }
                await _auditService.LogSecurityAsync("Password Reset", $"Admin reset password for user {user.Email}", user.Id, user.UserName);
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
                await _auditService.LogAsync("Role Change", "User", user.Id, string.Join(",", currentRoles), model.Role, $"Updated user role to {model.Role}");
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // POST: Users/Unlock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogSecurityAsync("Account Unlocked", $"Administrator manually unlocked account for user {user.Email}", user.Id, user.UserName);
            TempData["SuccessMessage"] = $"Account for '{user.FullName}' unlocked successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync("Status Change", "User", user.Id, (!user.IsActive).ToString(), user.IsActive.ToString(), $"Toggled user active status to {user.IsActive}");
            TempData["SuccessMessage"] = $"User status set to {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
