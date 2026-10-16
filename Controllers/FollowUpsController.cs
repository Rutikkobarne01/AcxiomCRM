using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
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

        // GET: FollowUps
        public async Task<IActionResult> Index(string? status, string? type, string? time, int page = 1)
        {
            int pageSize = 10;
            var query = await _crmService.GetScopedFollowUpsAsync(User);
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            if (time == "Upcoming")
            {
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= now);
            }
            else if (time == "Overdue")
            {
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate < now);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new FollowUpListViewModel
            {
                FollowUps = items,
                StatusFilter = status,
                TypeFilter = type,
                TimeFilter = time,
                PageNumber = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        // GET: FollowUps/Create
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            await PopulateDropdownsAsync(customerId, leadId);
            var model = new FollowUpCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.UtcNow.AddDays(1).Date.AddHours(11), // Default tomorrow 11 AM
                FollowUpType = "Call",
                Status = "Planned"
            };
            return View(model);
        }

        // POST: FollowUps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
        {
            // Business Validation Rule: Cannot be earlier than today for new/planned follow-up
            if (model.Status == "Planned" && model.FollowUpDate < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId, model.LeadId);
                return View(model);
            }

            var currentUserId = (await _userManager.GetUserAsync(User))?.Id;
            var assignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : currentUserId;

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject.Trim(),
                Remarks = model.Remarks?.Trim(),
                Status = model.Status,
                AssignedTo = assignedTo,
                CreatedDate = DateTime.UtcNow
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                null,
                $"Subject: {followUp.Subject}, Type: {followUp.FollowUpType}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}",
                $"Scheduled new {followUp.FollowUpType} follow-up");

            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? completionNotes)
        {
            var query = await _crmService.GetScopedFollowUpsAsync(User);
            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            var oldStatus = followUp.Status;
            followUp.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(completionNotes))
            {
                followUp.Remarks = $"{followUp.Remarks}\n[Completed Notes: {completionNotes}]".Trim();
            }

            // Create completed activity record
            _context.Activities.Add(new Activity
            {
                ActivityType = followUp.FollowUpType,
                Subject = $"Completed: {followUp.Subject}",
                Description = followUp.Remarks,
                ActivityDate = DateTime.UtcNow,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                AssignedTo = followUp.AssignedTo,
                Status = "Completed"
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Complete",
                "FollowUp",
                id.ToString(),
                $"Status: {oldStatus}",
                "Status: Completed",
                $"Follow-up '{followUp.Subject}' marked as completed");

            TempData["SuccessMessage"] = "Follow-up marked as Completed!";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? rescheduleReason)
        {
            var query = await _crmService.GetScopedFollowUpsAsync(User);
            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            if (newDate < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "New follow-up date cannot be earlier than today.";
                return RedirectToAction(nameof(Index));
            }

            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newDate;
            followUp.Status = "Planned";
            if (!string.IsNullOrWhiteSpace(rescheduleReason))
            {
                followUp.Remarks = $"{followUp.Remarks}\n[Rescheduled: {rescheduleReason}]".Trim();
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Reschedule",
                "FollowUp",
                id.ToString(),
                $"Date: {oldDate:yyyy-MM-dd HH:mm}",
                $"Date: {newDate:yyyy-MM-dd HH:mm}",
                $"Rescheduled follow-up '{followUp.Subject}'");

            TempData["SuccessMessage"] = "Follow-up successfully rescheduled.";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var query = await _crmService.GetScopedFollowUpsAsync(User);
            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return NotFound();

            followUp.Status = "Cancelled";
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Cancel", "FollowUp", id.ToString(), "Status: Planned", "Status: Cancelled", $"Cancelled follow-up '{followUp.Subject}'");

            TempData["SuccessMessage"] = "Follow-up cancelled.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedCustomerId = null, int? selectedLeadId = null)
        {
            var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

            var leads = await _context.Leads.Where(l => l.Status != "Lost").OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", selectedLeadId);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName");
        }
    }
}
