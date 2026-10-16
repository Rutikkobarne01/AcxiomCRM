namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityName, string? recordId, string? oldValue, string? newValue, string? details = null);
        Task LogSecurityAsync(string action, string details, string? userId = null, string? userName = null);
    }
}
