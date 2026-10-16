using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        // KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }

        // Chart Data (JSON-serializable)
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        public List<string> PipelineStageLabels { get; set; } = new();
        public List<decimal> PipelineStageAmounts { get; set; } = new();
        public List<int> PipelineStageCounts { get; set; } = new();

        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesValues { get; set; } = new();

        // Recent items
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<FollowUp> OverdueFollowUps { get; set; } = new();
        public List<Activity> RecentActivities { get; set; } = new();

        // Filter
        public string DateRangeFilter { get; set; } = "All"; // Today, Week, Month, All
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
