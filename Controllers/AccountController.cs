using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email or username
            var user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                       ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

            if (user == null)
            {
                await _auditService.LogSecurityAsync("Failed Login", $"Attempted login with non-existent user: {model.EmailOrUsername}");
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                await _auditService.LogSecurityAsync("Failed Login", $"Inactive account login attempt: {user.Email}", user.Id, user.UserName);
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an Administrator.");
                return View(model);
            }

            // Check lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var minutesRemaining = lockoutEnd.HasValue ? Math.Max(1, (int)(lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes) : 15;
                await _auditService.LogSecurityAsync("Failed Login", $"Account is locked out. Remaining: {minutesRemaining} min. User: {user.Email}", user.Id, user.UserName);
                ModelState.AddModelError(string.Empty, $"Account is temporarily locked due to multiple failed login attempts. Please try again after {minutesRemaining} minute(s) or contact an Admin.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogSecurityAsync("Login Success", $"User logged in successfully: {user.Email}", user.Id, user.UserName);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogSecurityAsync("Account Lockout Triggered", $"User locked out after exceeding failed attempts limit: {user.Email}", user.Id, user.UserName);
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed attempts. Please wait 15 minutes or ask an Admin to unlock.");
                return View(model);
            }

            // Record failed attempt
            int failedCount = await _userManager.GetAccessFailedCountAsync(user);
            await _auditService.LogSecurityAsync("Failed Login", $"Incorrect password attempt ({failedCount}/5): {user.Email}", user.Id, user.UserName);
            ModelState.AddModelError(string.Empty, $"Invalid login attempt. Failed count: {failedCount}/5. Account will lock on 5th failure.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                FullName = model.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                string assignedRole = "SalesExecutive";
                if (User.IsInRole("Admin") && !string.IsNullOrEmpty(model.Role))
                {
                    assignedRole = model.Role;
                }

                if (!await _roleManager.RoleExistsAsync(assignedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(assignedRole));
                }

                await _userManager.AddToRoleAsync(user, assignedRole);
                await _auditService.LogAsync("Create", "User", user.Id, null, $"User registered: {user.Email}, Role: {assignedRole}", "Self-registration");

                // Auto sign-in if guest registering
                if (User.Identity?.IsAuthenticated != true)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                TempData["SuccessMessage"] = "User registered successfully.";
                return RedirectToAction("Index", "Users");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            await _signInManager.SignOutAsync();
            if (user != null)
            {
                await _auditService.LogSecurityAsync("Logout", $"User logged out: {user.Email}", user.Id, user.UserName);
            }
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
