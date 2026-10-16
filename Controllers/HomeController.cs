using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ICrmService crmService,
            UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string dateRange = "All")
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var roleName = roles.FirstOrDefault() ?? "SalesExecutive";

            var customersQuery = await _crmService.GetScopedCustomersAsync(User);
            var leadsQuery = await _crmService.GetScopedLeadsAsync(User);
            var opportunitiesQuery = await _crmService.GetScopedOpportunitiesAsync(User);
            var followUpsQuery = await _crmService.GetScopedFollowUpsAsync(User);
            var activitiesQuery = await _crmService.GetScopedActivitiesAsync(User);

            // Optional Date Filtering
            var now = DateTime.UtcNow;
            if (dateRange == "Today")
            {
                var startOfDay = now.Date;
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startOfDay);
                opportunitiesQuery = opportunitiesQuery.Where(o => o.CreatedDate >= startOfDay);
            }
            else if (dateRange == "Week")
            {
                var startOfWeek = now.AddDays(-7);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startOfWeek);
                opportunitiesQuery = opportunitiesQuery.Where(o => o.CreatedDate >= startOfWeek);
            }
            else if (dateRange == "Month")
            {
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startOfMonth);
                opportunitiesQuery = opportunitiesQuery.Where(o => o.CreatedDate >= startOfMonth);
            }

            var customers = await customersQuery.ToListAsync();
            var leads = await leadsQuery.ToListAsync();
            var opportunities = await opportunitiesQuery.ToListAsync();

            // Calculate KPIs
            var totalCustomers = customers.Count;
            var totalLeads = leads.Count;
            var openLeads = leads.Count(l => l.Status != "Converted" && l.Status != "Lost");
            var totalOpportunities = opportunities.Count;
            var openOpportunities = opportunities.Count(o => o.Status == "Open" || (o.Stage != "Won" && o.Stage != "Lost"));
            var wonOpportunities = opportunities.Count(o => o.Status == "Won" || o.Stage == "Won");
            var lostOpportunities = opportunities.Count(o => o.Status == "Lost" || o.Stage == "Lost");

            var eligibleOpenOpps = opportunities.Where(o => o.Status == "Open" && o.Stage != "Won" && o.Stage != "Lost").ToList();
            var totalPipelineValue = eligibleOpenOpps.Sum(o => o.Amount);
            var weightedPipelineValue = eligibleOpenOpps.Sum(o => (o.Amount * o.Probability) / 100m);

            // Lead Status Chart Data
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
            var leadStatusCounts = leadStatuses.Select(st => leads.Count(l => l.Status == st)).ToList();

            // Opportunity Pipeline Chart Data
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var pipelineAmounts = stages.Select(st => opportunities.Where(o => o.Stage == st).Sum(o => o.Amount)).ToList();
            var pipelineCounts = stages.Select(st => opportunities.Count(o => o.Stage == st)).ToList();

            // Monthly Sales Trend Data (Past 6 Months)
            var monthlyLabels = new List<string>();
            var monthlyValues = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                monthlyLabels.Add(targetMonth.ToString("MMM yyyy"));
                var monthSum = opportunities
                    .Where(o => (o.Status == "Won" || o.Stage == "Won") &&
                                o.CreatedDate.Month == targetMonth.Month &&
                                o.CreatedDate.Year == targetMonth.Year)
                    .Sum(o => o.Amount);

                monthlyValues.Add(monthSum);
            }

            // Upcoming & Overdue Follow-ups
            var allFollowUps = await followUpsQuery.ToListAsync();
            var upcomingFollowUps = allFollowUps
                .Where(f => f.Status == "Planned" && f.FollowUpDate >= now)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToList();

            var overdueFollowUps = allFollowUps
                .Where(f => f.Status == "Planned" && f.FollowUpDate < now)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToList();

            var recentActivities = await activitiesQuery
                .OrderByDescending(a => a.ActivityDate)
                .Take(6)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalCustomers = totalCustomers,
                TotalLeads = totalLeads,
                OpenLeads = openLeads,
                TotalOpportunities = totalOpportunities,
                OpenOpportunities = openOpportunities,
                WonOpportunities = wonOpportunities,
                LostOpportunities = lostOpportunities,
                TotalPipelineValue = totalPipelineValue,
                WeightedPipelineValue = weightedPipelineValue,

                LeadStatusLabels = leadStatuses.ToList(),
                LeadStatusCounts = leadStatusCounts,

                PipelineStageLabels = stages.ToList(),
                PipelineStageAmounts = pipelineAmounts,
                PipelineStageCounts = pipelineCounts,

                MonthlySalesLabels = monthlyLabels,
                MonthlySalesValues = monthlyValues,

                UpcomingFollowUps = upcomingFollowUps,
                OverdueFollowUps = overdueFollowUps,
                RecentActivities = recentActivities,

                DateRangeFilter = dateRange,
                UserRole = roleName,
                UserName = user?.FullName ?? user?.UserName ?? "User"
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
