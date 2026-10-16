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
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
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

        // GET: Customers
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            int pageSize = 10;
            var query = await _crmService.GetScopedCustomersAsync(User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CustomerName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                    c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CustomerListViewModel
            {
                Customers = items,
                SearchTerm = search,
                StatusFilter = status,
                PageNumber = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount
            };

            return View(viewModel);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedCustomersAsync(User);
            var customer = await query
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            var opportunities = await _context.Opportunities
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var followUps = await _context.FollowUps
                .Where(f => f.CustomerId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.CustomerId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var auditHistory = await _context.AuditLogs
                .Where(a => a.EntityName == "Customer" && a.RecordId == id.ToString())
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            var viewModel = new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = opportunities,
                FollowUps = followUps,
                Activities = activities,
                AuditHistory = auditHistory
            };

            return View(viewModel);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUsersDropdownAsync();
            var model = new CustomerCreateEditViewModel
            {
                CustomerCode = await _crmService.GenerateCustomerCodeAsync(),
                Status = "Active"
            };
            return View(model);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
        {
            // Business Validation: Duplicate email or phone check
            if (await _crmService.IsCustomerDuplicateAsync(model.Email, model.Phone))
            {
                ModelState.AddModelError(string.Empty, "Duplicate customer detected! A customer with this email or phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync();
                return View(model);
            }

            var currentUserId = (await _userManager.GetUserAsync(User))?.Id;
            var assignedTo = !string.IsNullOrEmpty(model.CreatedBy) ? model.CreatedBy : currentUserId;
            var code = string.IsNullOrWhiteSpace(model.CustomerCode) ? await _crmService.GenerateCustomerCodeAsync() : model.CustomerCode;

            var customer = new Customer
            {
                CustomerCode = code,
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = assignedTo
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Audit
            await _auditService.LogAsync(
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                $"Name: {customer.CustomerName}, Email: {customer.Email}, Code: {customer.CustomerCode}",
                $"Customer {customer.CustomerName} created successfully");

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' ({customer.CustomerCode}) created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = await _crmService.GetScopedCustomersAsync(User);
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            await PopulateUsersDropdownAsync(customer.CreatedBy);

            var model = new CustomerCreateEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedBy = customer.CreatedBy
            };

            return View(model);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerCreateEditViewModel model)
        {
            if (id != model.CustomerId) return NotFound();

            var query = await _crmService.GetScopedCustomersAsync(User);
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            // Duplicate check excluding this record
            if (await _crmService.IsCustomerDuplicateAsync(model.Email, model.Phone, id))
            {
                ModelState.AddModelError(string.Empty, "Another customer already has this email or phone number.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model.CreatedBy);
                return View(model);
            }

            var oldValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Phone: {customer.Phone}, Status: {customer.Status}";

            customer.CustomerName = model.CustomerName.Trim();
            customer.Email = model.Email.Trim().ToLower();
            customer.Phone = model.Phone.Trim();
            customer.CompanyName = model.CompanyName?.Trim();
            customer.Address = model.Address?.Trim();
            customer.City = model.City?.Trim();
            customer.State = model.State?.Trim();
            customer.Status = model.Status;
            if (!string.IsNullOrEmpty(model.CreatedBy))
            {
                customer.CreatedBy = model.CreatedBy;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Phone: {customer.Phone}, Status: {customer.Status}";

            await _auditService.LogAsync(
                "Update",
                "Customer",
                customer.CustomerId.ToString(),
                oldValue,
                newValue,
                $"Updated customer {customer.CustomerName}");

            TempData["SuccessMessage"] = "Customer details updated successfully.";
            return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = await _crmService.GetScopedCustomersAsync(User);
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            // Soft-deactivate if has linked deals or delete if clean
            bool hasDependencies = await _context.Opportunities.AnyAsync(o => o.CustomerId == id)
                                || await _context.FollowUps.AnyAsync(f => f.CustomerId == id);

            if (hasDependencies)
            {
                customer.Status = "Inactive";
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Deactivate", "Customer", id.ToString(), "Status: Active", "Status: Inactive", "Customer deactivated due to associated sales history");
                TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' deactivated (retained sales history).";
            }
            else
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Delete", "Customer", id.ToString(), $"Customer {customer.CustomerName}", null, "Customer removed from database");
                TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(string? selectedId = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
