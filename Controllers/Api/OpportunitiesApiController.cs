using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/opportunities")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public OpportunitiesApiController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? stage)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            var opps = await query.OrderByDescending(o => o.CreatedDate).Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedValue = (o.Amount * o.Probability) / 100m,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedUserName = o.AssignedUser != null ? o.AssignedUser.FullName : null,
                Notes = o.Notes
            }).ToListAsync();

            return Ok(opps);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            // Business Validation Rules
            if (dto.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Opportunity Amount must be greater than 0." });
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(new { success = false, message = "Probability must be between 0 and 100." });
            }

            if (dto.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new { success = false, message = "Expected Close Date cannot be in the past." });
            }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName.Trim(),
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Stage == "Won" ? "Won" : (dto.Stage == "Lost" ? "Lost" : "Open"),
                Notes = dto.Notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create (API)", "Opportunity", opp.OpportunityId.ToString(), null, $"Deal: {opp.OpportunityName}, ₹{opp.Amount}", "Created deal via REST API");

            var resultDto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedValue = opp.WeightedValue,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                Notes = opp.Notes
            };

            return CreatedAtAction(nameof(GetOpportunities), new { id = opp.OpportunityId }, resultDto);
        }
    }
}
