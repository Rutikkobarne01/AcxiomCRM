using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class CrmService : ICrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public CrmService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        private async Task<(string UserId, bool IsAdmin, bool IsManager, bool IsSales)> GetUserRoleInfoAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                return (string.Empty, false, false, false);
            }

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");
            bool isManager = roles.Contains("Manager");
            bool isSales = roles.Contains("SalesExecutive");

            return (user.Id, isAdmin, isManager, isSales);
        }

        public async Task<IQueryable<Customer>> GetScopedCustomersAsync(ClaimsPrincipal userPrincipal)
        {
            var (userId, isAdmin, isManager, isSales) = await GetUserRoleInfoAsync(userPrincipal);
            var query = _context.Customers
                .Include(c => c.Owner)
                .AsQueryable();

            if (isAdmin || isManager)
            {
                return query;
            }

            if (isSales && !string.IsNullOrEmpty(userId))
            {
                return query.Where(c => c.CreatedBy == userId);
            }

            return query;
        }

        public async Task<IQueryable<Lead>> GetScopedLeadsAsync(ClaimsPrincipal userPrincipal)
        {
            var (userId, isAdmin, isManager, isSales) = await GetUserRoleInfoAsync(userPrincipal);
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .AsQueryable();

            if (isAdmin || isManager)
            {
                return query;
            }

            if (isSales && !string.IsNullOrEmpty(userId))
            {
                return query.Where(l => l.AssignedTo == userId);
            }

            return query;
        }

        public async Task<IQueryable<Opportunity>> GetScopedOpportunitiesAsync(ClaimsPrincipal userPrincipal)
        {
            var (userId, isAdmin, isManager, isSales) = await GetUserRoleInfoAsync(userPrincipal);
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (isAdmin || isManager)
            {
                return query;
            }

            if (isSales && !string.IsNullOrEmpty(userId))
            {
                return query.Where(o => o.AssignedTo == userId);
            }

            return query;
        }

        public async Task<IQueryable<FollowUp>> GetScopedFollowUpsAsync(ClaimsPrincipal userPrincipal)
        {
            var (userId, isAdmin, isManager, isSales) = await GetUserRoleInfoAsync(userPrincipal);
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (isAdmin || isManager)
            {
                return query;
            }

            if (isSales && !string.IsNullOrEmpty(userId))
            {
                return query.Where(f => f.AssignedTo == userId);
            }

            return query;
        }

        public async Task<IQueryable<Activity>> GetScopedActivitiesAsync(ClaimsPrincipal userPrincipal)
        {
            var (userId, isAdmin, isManager, isSales) = await GetUserRoleInfoAsync(userPrincipal);
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            if (isAdmin || isManager)
            {
                return query;
            }

            if (isSales && !string.IsNullOrEmpty(userId))
            {
                return query.Where(a => a.AssignedTo == userId);
            }

            return query;
        }

        public async Task<bool> IsCustomerDuplicateAsync(string email, string phone, int? currentCustomerId = null)
        {
            var normalizedEmail = email.Trim().ToLower();
            var normalizedPhone = phone.Trim().Replace(" ", "").Replace("-", "");

            return await _context.Customers.AnyAsync(c =>
                (currentCustomerId == null || c.CustomerId != currentCustomerId) &&
                (c.Email.ToLower() == normalizedEmail ||
                 c.Phone.Replace(" ", "").Replace("-", "") == normalizedPhone));
        }

        public async Task<string> GenerateCustomerCodeAsync()
        {
            int count = await _context.Customers.CountAsync();
            return $"CUST-{(count + 1):D4}";
        }

        public async Task<string> GenerateLeadCodeAsync()
        {
            int count = await _context.Leads.CountAsync();
            return $"LEAD-{(count + 1):D4}";
        }

        public async Task<(Customer Customer, Opportunity? Opportunity)> ConvertLeadAsync(
            int leadId,
            ClaimsPrincipal userPrincipal,
            bool createOpportunity = true,
            decimal opportunityAmount = 0,
            string? opportunityStage = "Qualification")
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
            {
                throw new InvalidOperationException("Lead not found.");
            }

            if (lead.Status == "Converted")
            {
                throw new InvalidOperationException("This lead has already been converted.");
            }

            var user = await _userManager.GetUserAsync(userPrincipal);
            var assignedUserId = lead.AssignedTo ?? user?.Id;

            // 1. Create Customer
            var customerCode = await GenerateCustomerCodeAsync();
            var customer = new Customer
            {
                CustomerCode = customerCode,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Address = "Imported from Lead " + lead.LeadCode,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = assignedUserId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // 2. Create Opportunity if requested
            Opportunity? opportunity = null;
            if (createOpportunity)
            {
                var amount = opportunityAmount > 0 ? opportunityAmount : (lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000);
                opportunity = new Opportunity
                {
                    OpportunityName = $"Deal with {customer.CustomerName}",
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = amount,
                    Stage = string.IsNullOrWhiteSpace(opportunityStage) ? "Qualification" : opportunityStage,
                    Probability = 25,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = assignedUserId,
                    Notes = $"Auto-generated upon conversion of Lead {lead.LeadCode}."
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();
            }

            // 3. Update Lead Status
            var oldStatus = lead.Status;
            lead.Status = "Converted";
            lead.ConvertedCustomerId = customer.CustomerId;
            if (opportunity != null)
            {
                lead.ConvertedOpportunityId = opportunity.OpportunityId;
            }

            // 4. Log Activity
            var activity = new Activity
            {
                ActivityType = "Task",
                Subject = $"Lead Converted: {lead.LeadCode} to Customer {customer.CustomerCode}",
                Description = $"Lead was converted by {user?.FullName ?? user?.UserName ?? "System"}. Opportunity ID: {opportunity?.OpportunityId.ToString() ?? "None"}.",
                ActivityDate = DateTime.UtcNow,
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                AssignedTo = assignedUserId,
                Status = "Completed"
            };
            _context.Activities.Add(activity);

            await _context.SaveChangesAsync();

            // 5. Audit Log
            await _auditService.LogAsync(
                "Lead Conversion",
                "Lead",
                lead.LeadId.ToString(),
                $"Status: {oldStatus}",
                $"Status: Converted, CustomerId: {customer.CustomerId}, OppId: {opportunity?.OpportunityId}",
                $"Converted lead {lead.LeadName} ({lead.LeadCode}) to customer {customer.CustomerName} ({customer.CustomerCode})");

            return (customer, opportunity);
        }
    }
}
