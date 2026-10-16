using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? type, string? search)
        {
            var query = await _crmService.GetScopedActivitiesAsync(User);

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Subject.ToLower().Contains(term) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .Take(50)
                .ToListAsync();

            ViewBag.SelectedType = type;
            ViewBag.SearchTerm = search;
            return View(activities);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            await PopulateDropdownsAsync(customerId, leadId);
            var activity = new Activity
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.UtcNow,
                ActivityType = "Call",
                Status = "Completed"
            };
            return View(activity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(activity.CustomerId, activity.LeadId);
                return View(activity);
            }

            var user = await _userManager.GetUserAsync(User);
            activity.AssignedTo ??= user?.Id;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create", "Activity", activity.ActivityId.ToString(), null, $"Activity: {activity.Subject} ({activity.ActivityType})", "Logged CRM activity");

            TempData["SuccessMessage"] = "Activity logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedCustomerId = null, int? selectedLeadId = null)
        {
            var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

            var leads = await _context.Leads.Where(l => l.Status != "Lost").OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", selectedLeadId);
        }
    }
}
