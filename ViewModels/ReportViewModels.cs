using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "Pipeline"; // Pipeline, Customers, Leads, Opportunities, FollowUps, Conversion, UserActivity
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StatusFilter { get; set; }
        public string? OwnerFilter { get; set; }
    }

    public class PipelineReportItem
    {
        public string Stage { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
        public decimal AverageProbability { get; set; }
    }

    public class OwnerPipelineReportItem
    {
        public string OwnerName { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportSummary
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int ActiveLeads { get; set; }
        public decimal ConversionRate => TotalLeads > 0 ? ((decimal)ConvertedLeads / TotalLeads) * 100m : 0m;
        public int TotalOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal WinRate => (WonOpportunities + LostOpportunities) > 0
            ? ((decimal)WonOpportunities / (WonOpportunities + LostOpportunities)) * 100m
            : 0m;
    }

    public class UserActivityReportItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CustomersCount { get; set; }
        public int LeadsCount { get; set; }
        public int OpportunitiesCount { get; set; }
        public int CompletedFollowUpsCount { get; set; }
        public int TotalAuditActions { get; set; }
    }

    public class MasterReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new();
        public List<PipelineReportItem> PipelineByStage { get; set; } = new();
        public List<OwnerPipelineReportItem> PipelineByOwner { get; set; } = new();
        public ConversionReportSummary ConversionSummary { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Lead> Leads { get; set; } = new();
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<UserActivityReportItem> UserActivities { get; set; } = new();
    }
}
