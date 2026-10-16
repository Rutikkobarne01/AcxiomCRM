using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditService(
            ApplicationDbContext _context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            this._context = _context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(string action, string entityName, string? recordId, string? oldValue, string? newValue, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var userId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = httpContext?.User?.Identity?.Name ?? "System";
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var log = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Details = details
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never allow audit logging failure to crash core operations
            }
        }

        public async Task LogSecurityAsync(string action, string details, string? userId = null, string? userName = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var currentUserId = userId ?? httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUserName = userName ?? httpContext?.User?.Identity?.Name ?? "Anonymous";
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var log = new AuditLog
                {
                    UserId = currentUserId,
                    UserName = currentUserName,
                    Action = action,
                    EntityName = "Security",
                    RecordId = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Details = details
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Ignore audit failure
            }
        }
    }
}
