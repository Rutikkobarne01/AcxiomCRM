using System.Security.Claims;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface ICrmService
    {
        Task<IQueryable<Customer>> GetScopedCustomersAsync(ClaimsPrincipal user);
        Task<IQueryable<Lead>> GetScopedLeadsAsync(ClaimsPrincipal user);
        Task<IQueryable<Opportunity>> GetScopedOpportunitiesAsync(ClaimsPrincipal user);
        Task<IQueryable<FollowUp>> GetScopedFollowUpsAsync(ClaimsPrincipal user);
        Task<IQueryable<Activity>> GetScopedActivitiesAsync(ClaimsPrincipal user);

        Task<bool> IsCustomerDuplicateAsync(string email, string phone, int? currentCustomerId = null);
        Task<string> GenerateCustomerCodeAsync();
        Task<string> GenerateLeadCodeAsync();

        Task<(Customer Customer, Opportunity? Opportunity)> ConvertLeadAsync(
            int leadId,
            ClaimsPrincipal user,
            bool createOpportunity = true,
            decimal opportunityAmount = 0,
            string? opportunityStage = "Qualification");
    }
}
