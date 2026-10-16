using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 20;
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                var term = userFilter.Trim().ToLower();
                query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(term)) ||
                                         (a.UserId != null && a.UserId.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                query = query.Where(a => a.EntityName == moduleFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            int totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.UserFilter = userFilter;
            ViewBag.ModuleFilter = moduleFilter;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            ViewBag.DistinctModules = await _context.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();
            ViewBag.DistinctActions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();

            return View(logs);
        }
    }
}
