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
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
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

        // GET: Opportunities
        public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
        {
            int pageSize = 10;
            var query = await _crmService.GetScopedOpportunitiesAsync(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OpportunityName.ToLower().Contains(term) ||
                    (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                    (o.Notes != null && o.Notes.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var allMatching = await query.ToListAsync();
            var totalPipeline = allMatching.Where(o => o.Status == "Open").Sum(o => o.Amount);
            var weightedPipeline = allMatching.Where(o => o.Status == "Open").Sum(o => (o.Amount * o.Probability) / 100m);

            int totalCount = allMatching.Count;
            var items = allMatching
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new OpportunityListViewModel
            {
                Opportunities = items,
                SearchTerm = search,
                StageFilter = stage,
                StatusFilter = status,
                TotalPipelineAmount = totalPipeline,
                TotalWeightedAmount = weightedPipeline,
                PageNumber = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        // GET: Opportunities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedOpportunitiesAsync(User);
            var opportunity = await query
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            ViewBag.AuditLogs = await _context.AuditLogs
                .Where(a => a.EntityName == "Opportunity" && a.RecordId == id.ToString())
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(opportunity);
        }

        // GET: Opportunities/Create
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            await PopulateDropdownsAsync(customerId);
            var model = new OpportunityCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                Stage = "Qualification",
                Probability = 20,
                Amount = 10000,
                Status = "Open"
            };
            return View(model);
        }

        // POST: Opportunities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
        {
            // Business Validation Rules (Section 5.3 & 17.7)
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for active opportunities.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId);
                return View(model);
            }

            var currentUserId = (await _userManager.GetUserAsync(User))?.Id;
            var assignedTo = !string.IsNullOrEmpty(model.AssignedTo) ? model.AssignedTo : currentUserId;

            // Automatically sync Status with Stage
            var status = model.Status;
            if (model.Stage == "Won") status = "Won";
            else if (model.Stage == "Lost") status = "Lost";

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = status,
                AssignedTo = assignedTo,
                Notes = model.Notes?.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            // Activity
            _context.Activities.Add(new Activity
            {
                ActivityType = "Task",
                Subject = $"Opportunity Created: {opp.OpportunityName}",
                Description = $"Created deal with value ₹{opp.Amount:N0} at stage {opp.Stage} ({opp.Probability}% win probability)",
                ActivityDate = DateTime.UtcNow,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                AssignedTo = assignedTo,
                Status = "Completed"
            });
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Create",
                "Opportunity",
                opp.OpportunityId.ToString(),
                null,
                $"Deal: {opp.OpportunityName}, Amount: ₹{opp.Amount}, Stage: {opp.Stage}",
                $"Created opportunity deal {opp.OpportunityName}");

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Opportunities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedOpportunitiesAsync(User);
            var opp = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opp == null) return NotFound();

            await PopulateDropdownsAsync(opp.CustomerId, opp.AssignedTo);

            var model = new OpportunityCreateEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes
            };

            return View(model);
        }

        // POST: Opportunities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel model)
        {
            if (id != model.OpportunityId) return NotFound();

            var query = await _crmService.GetScopedOpportunitiesAsync(User);
            var opp = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opp == null) return NotFound();

            // Business Validation Rules
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for an active Opportunity.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
                return View(model);
            }

            var oldValue = $"Amount: {opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}%, Close: {opp.ExpectedCloseDate:yyyy-MM-dd}";

            // Stage-Status consistency
            var status = model.Status;
            if (model.Stage == "Won")
            {
                status = "Won";
                model.Probability = 100;
            }
            else if (model.Stage == "Lost")
            {
                status = "Lost";
                model.Probability = 0;
            }

            opp.OpportunityName = model.OpportunityName.Trim();
            opp.CustomerId = model.CustomerId;
            opp.Amount = model.Amount;
            opp.Stage = model.Stage;
            opp.Probability = model.Probability;
            opp.ExpectedCloseDate = model.ExpectedCloseDate;
            opp.Status = status;
            opp.Notes = model.Notes?.Trim();
            if (!string.IsNullOrEmpty(model.AssignedTo))
            {
                opp.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Amount: {opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}%, Close: {opp.ExpectedCloseDate:yyyy-MM-dd}";

            await _auditService.LogAsync(
                "Update",
                "Opportunity",
                opp.OpportunityId.ToString(),
                oldValue,
                newValue,
                $"Updated opportunity {opp.OpportunityName}");

            TempData["SuccessMessage"] = "Opportunity deal updated successfully.";
            return RedirectToAction(nameof(Details), new { id = opp.OpportunityId });
        }

        // POST: Opportunities/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = await _crmService.GetScopedOpportunitiesAsync(User);
            var opp = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opp == null) return NotFound();

            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Opportunity", id.ToString(), $"Deal {opp.OpportunityName}", null, "Deleted sales opportunity");
            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedCustomerId = null, string? selectedUserId = null)
        {
            var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUserId);
        }
    }
}
