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
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(
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

        // GET: Leads
        public async Task<IActionResult> Index(string? search, string? status, string? priority, string? source, int page = 1)
        {
            int pageSize = 10;
            var query = await _crmService.GetScopedLeadsAsync(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l =>
                    l.LeadName.ToLower().Contains(term) ||
                    l.Email.ToLower().Contains(term) ||
                    l.Phone.Contains(term) ||
                    (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                    l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(l => l.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                query = query.Where(l => l.Source == source);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new LeadListViewModel
            {
                Leads = items,
                SearchTerm = search,
                StatusFilter = status,
                PriorityFilter = priority,
                SourceFilter = source,
                PageNumber = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        // GET: Leads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedLeadsAsync(User);
            var lead = await query
                .Include(l => l.AssignedUser)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return NotFound();

            ViewBag.FollowUps = await _context.FollowUps
                .Where(f => f.LeadId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            ViewBag.Activities = await _context.Activities
                .Where(a => a.LeadId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            return View(lead);
        }

        // GET: Leads/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUsersDropdownAsync();
            var model = new LeadCreateEditViewModel
            {
                LeadCode = await _crmService.GenerateLeadCodeAsync(),
                Status = "New",
                Priority = "Medium",
                Source = "Website"
            };
            return View(model);
        }

        // POST: Leads/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync();
                return View(model);
            }

            var currentUserId = (await _userManager.GetUserAsync(User))?.Id;
            var assignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : currentUserId;
            var code = string.IsNullOrWhiteSpace(model.LeadCode) ? await _crmService.GenerateLeadCodeAsync() : model.LeadCode;

            var lead = new Lead
            {
                LeadCode = code,
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = assignedTo
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            // Initial activity
            _context.Activities.Add(new Activity
            {
                ActivityType = "Task",
                Subject = $"New Lead Captured: {lead.LeadName}",
                Description = $"Lead created from source {lead.Source} with priority {lead.Priority}",
                ActivityDate = DateTime.UtcNow,
                LeadId = lead.LeadId,
                AssignedTo = assignedTo,
                Status = "Completed"
            });
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                null,
                $"Lead: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}",
                $"Captured new lead {lead.LeadName} ({lead.LeadCode})");

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' ({lead.LeadCode}) created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Leads/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedLeadsAsync(User);
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            await PopulateUsersDropdownAsync(lead.AssignedTo);

            var model = new LeadCreateEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo
            };

            return View(model);
        }

        // POST: Leads/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadCreateEditViewModel model)
        {
            if (id != model.LeadId) return NotFound();

            var query = await _crmService.GetScopedLeadsAsync(User);
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            // Check if already converted
            if (lead.Status == "Converted" && model.Status != "Converted")
            {
                ModelState.AddModelError("Status", "A converted lead cannot be moved backwards to another status.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model.AssignedTo);
                return View(model);
            }

            var oldValue = $"Name: {lead.LeadName}, Status: {lead.Status}, Priority: {lead.Priority}, ExpectedValue: {lead.ExpectedValue}";

            lead.LeadName = model.LeadName.Trim();
            lead.Email = model.Email.Trim().ToLower();
            lead.Phone = model.Phone.Trim();
            lead.CompanyName = model.CompanyName?.Trim();
            lead.Source = model.Source;
            lead.Status = model.Status;
            lead.Priority = model.Priority;
            lead.ExpectedValue = model.ExpectedValue;
            if (!string.IsNullOrEmpty(model.AssignedTo))
            {
                lead.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {lead.LeadName}, Status: {lead.Status}, Priority: {lead.Priority}, ExpectedValue: {lead.ExpectedValue}";

            await _auditService.LogAsync("Update", "Lead", lead.LeadId.ToString(), oldValue, newValue, $"Updated lead {lead.LeadName}");

            TempData["SuccessMessage"] = "Lead updated successfully.";
            return RedirectToAction(nameof(Details), new { id = lead.LeadId });
        }

        // GET: Leads/Convert/5
        public async Task<IActionResult> Convert(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedLeadsAsync(User);
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "This lead is already converted.";
                return RedirectToAction(nameof(Details), new { id = lead.LeadId });
            }

            var model = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 50000,
                OpportunityStage = "Qualification",
                CreateOpportunity = true
            };

            return View(model);
        }

        // POST: Leads/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConvertViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (customer, opportunity) = await _crmService.ConvertLeadAsync(
                    model.LeadId,
                    User,
                    model.CreateOpportunity,
                    model.OpportunityAmount,
                    model.OpportunityStage);

                TempData["SuccessMessage"] = $"Lead converted successfully! Customer '{customer.CustomerName}' ({customer.CustomerCode}) created" +
                    (opportunity != null ? $" along with Opportunity deal of ₹{opportunity.Amount:N0}." : ".");

                return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // POST: Leads/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = await _crmService.GetScopedLeadsAsync(User);
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return NotFound();

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Lead", id.ToString(), $"Lead {lead.LeadName}", null, "Lead record deleted");
            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(string? selectedId = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
