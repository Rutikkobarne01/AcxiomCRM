using System.Text;
using AcxiomCRM.Data;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly UserManager<Models.Entities.ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            ICrmService crmService,
            UserManager<Models.Entities.ApplicationUser> userManager)
        {
            _context = context;
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string type = "Pipeline", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var model = new MasterReportViewModel
            {
                Filter = new ReportFilterViewModel
                {
                    ReportType = type,
                    FromDate = fromDate,
                    ToDate = toDate
                }
            };

            var customersQuery = await _crmService.GetScopedCustomersAsync(User);
            var leadsQuery = await _crmService.GetScopedLeadsAsync(User);
            var oppsQuery = await _crmService.GetScopedOpportunitiesAsync(User);
            var followUpsQuery = await _crmService.GetScopedFollowUpsAsync(User);

            if (fromDate.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate >= fromDate.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= fromDate.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate >= fromDate.Value);
                followUpsQuery = followUpsQuery.Where(f => f.FollowUpDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.AddDays(1).AddTicks(-1);
                customersQuery = customersQuery.Where(c => c.CreatedDate <= end);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate <= end);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate <= end);
                followUpsQuery = followUpsQuery.Where(f => f.FollowUpDate <= end);
            }

            var oppsList = await oppsQuery.Include(o => o.Customer).Include(o => o.AssignedUser).ToListAsync();
            var leadsList = await leadsQuery.Include(l => l.AssignedUser).ToListAsync();
            var customersList = await customersQuery.Include(c => c.Owner).ToListAsync();
            var followUpsList = await followUpsQuery.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).ToListAsync();

            model.Customers = customersList;
            model.Leads = leadsList;
            model.Opportunities = oppsList;
            model.FollowUps = followUpsList;

            // 1. Pipeline By Stage
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            model.PipelineByStage = stages.Select(st =>
            {
                var matching = oppsList.Where(o => o.Stage == st).ToList();
                return new PipelineReportItem
                {
                    Stage = st,
                    OpportunityCount = matching.Count,
                    TotalAmount = matching.Sum(m => m.Amount),
                    WeightedAmount = matching.Sum(m => (m.Amount * m.Probability) / 100m),
                    AverageProbability = matching.Any() ? (decimal)matching.Average(m => m.Probability) : 0m
                };
            }).ToList();

            // 2. Pipeline By Owner
            var groupedByOwner = oppsList.GroupBy(o => o.AssignedUser?.FullName ?? o.AssignedUser?.UserName ?? "Unassigned");
            model.PipelineByOwner = groupedByOwner.Select(g => new OwnerPipelineReportItem
            {
                OwnerName = g.Key,
                OpportunityCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => (x.Amount * x.Probability) / 100m)
            }).OrderByDescending(x => x.TotalAmount).ToList();

            // 3. Conversion Summary
            model.ConversionSummary = new ConversionReportSummary
            {
                TotalLeads = leadsList.Count,
                ConvertedLeads = leadsList.Count(l => l.Status == "Converted"),
                LostLeads = leadsList.Count(l => l.Status == "Lost"),
                ActiveLeads = leadsList.Count(l => l.Status != "Converted" && l.Status != "Lost"),
                TotalOpportunities = oppsList.Count,
                WonOpportunities = oppsList.Count(o => o.Status == "Won" || o.Stage == "Won"),
                LostOpportunities = oppsList.Count(o => o.Status == "Lost" || o.Stage == "Lost")
            };

            // 4. User Activity Summary (Admin & Manager view)
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                var activityItems = new List<UserActivityReportItem>();

                foreach (var u in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);
                    var uCustCount = await _context.Customers.CountAsync(c => c.CreatedBy == u.Id);
                    var uLeadCount = await _context.Leads.CountAsync(l => l.AssignedTo == u.Id);
                    var uOppCount = await _context.Opportunities.CountAsync(o => o.AssignedTo == u.Id);
                    var uFollowCount = await _context.FollowUps.CountAsync(f => f.AssignedTo == u.Id && f.Status == "Completed");
                    var uAuditCount = await _context.AuditLogs.CountAsync(a => a.UserId == u.Id);

                    activityItems.Add(new UserActivityReportItem
                    {
                        UserName = u.FullName,
                        Role = userRoles.FirstOrDefault() ?? "Staff",
                        CustomersCount = uCustCount,
                        LeadsCount = uLeadCount,
                        OpportunitiesCount = uOppCount,
                        CompletedFollowUpsCount = uFollowCount,
                        TotalAuditActions = uAuditCount
                    });
                }
                model.UserActivities = activityItems.OrderByDescending(x => x.TotalAuditActions).ToList();
            }

            return View(model);
        }

        // CSV Export for Reports
        public async Task<IActionResult> ExportCsv(string type = "Pipeline")
        {
            var sb = new StringBuilder();

            if (type == "Pipeline")
            {
                var opps = await (await _crmService.GetScopedOpportunitiesAsync(User))
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedUser)
                    .ToListAsync();

                sb.AppendLine("Opportunity ID,Name,Customer,Amount,Stage,Probability(%),Weighted Value,Expected Close Date,Status,Assigned To");
                foreach (var o in opps)
                {
                    sb.AppendLine($"\"{o.OpportunityId}\",\"{EscapeCsv(o.OpportunityName)}\",\"{EscapeCsv(o.Customer?.CustomerName ?? "")}\",\"{o.Amount}\",\"{o.Stage}\",\"{o.Probability}\",\"{o.WeightedValue}\",\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{o.Status}\",\"{EscapeCsv(o.AssignedUser?.FullName ?? "")}\"");
                }
                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"AcxiomCRM_PipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            else if (type == "Customers")
            {
                var customers = await (await _crmService.GetScopedCustomersAsync(User))
                    .Include(c => c.Owner)
                    .ToListAsync();

                sb.AppendLine("Code,Customer Name,Email,Phone,Company,City,State,Status,Created Date,Owner");
                foreach (var c in customers)
                {
                    sb.AppendLine($"\"{c.CustomerCode}\",\"{EscapeCsv(c.CustomerName)}\",\"{EscapeCsv(c.Email)}\",\"{c.Phone}\",\"{EscapeCsv(c.CompanyName ?? "")}\",\"{EscapeCsv(c.City ?? "")}\",\"{EscapeCsv(c.State ?? "")}\",\"{c.Status}\",\"{c.CreatedDate:yyyy-MM-dd}\",\"{EscapeCsv(c.Owner?.FullName ?? "")}\"");
                }
                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"AcxiomCRM_Customers_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            else if (type == "Leads")
            {
                var leads = await (await _crmService.GetScopedLeadsAsync(User))
                    .Include(l => l.AssignedUser)
                    .ToListAsync();

                sb.AppendLine("Code,Lead Name,Email,Phone,Company,Source,Status,Priority,Expected Value,Created Date,Assigned To");
                foreach (var l in leads)
                {
                    sb.AppendLine($"\"{l.LeadCode}\",\"{EscapeCsv(l.LeadName)}\",\"{EscapeCsv(l.Email)}\",\"{l.Phone}\",\"{EscapeCsv(l.CompanyName ?? "")}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",\"{l.ExpectedValue}\",\"{l.CreatedDate:yyyy-MM-dd}\",\"{EscapeCsv(l.AssignedUser?.FullName ?? "")}\"");
                }
                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"AcxiomCRM_Leads_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            return RedirectToAction(nameof(Index));
        }

        private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");
    }
}
