using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input payload", errors = ModelState });
            }

            var user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                       ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

            if (user == null || !user.IsActive)
            {
                await _auditService.LogSecurityAsync("API Login Failed", $"Non-existent or inactive user: {model.EmailOrUsername}");
                return Unauthorized(new { success = false, message = "Invalid credentials or inactive account." });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(423, new { success = false, message = "Account is locked out due to failed attempts." });
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _auditService.LogSecurityAsync("API Login Success", $"User logged in via REST API: {user.Email}", user.Id, user.UserName);

                return Ok(new
                {
                    success = true,
                    message = "Authentication successful",
                    user = new
                    {
                        user.Id,
                        user.FullName,
                        user.Email,
                        Roles = roles
                    }
                });
            }

            if (result.IsLockedOut)
            {
                return StatusCode(423, new { success = false, message = "Account has been locked out." });
            }

            return Unauthorized(new { success = false, message = "Invalid password." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }
}
