using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/leads")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public LeadsApiController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? status)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            var leads = await query.OrderByDescending(l => l.CreatedDate).Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedUserName = l.AssignedUser != null ? l.AssignedUser.FullName : null
            }).ToListAsync();

            return Ok(leads);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLead(int id)
        {
            var lead = await _context.Leads.Include(l => l.AssignedUser).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
            {
                return NotFound(new { success = false, message = $"Lead with ID {id} not found." });
            }

            var dto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate,
                AssignedUserName = lead.AssignedUser?.FullName
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            var code = await _crmService.GenerateLeadCodeAsync();
            var lead = new Lead
            {
                LeadCode = code,
                LeadName = dto.LeadName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Source = dto.Source ?? "Website",
                Status = dto.Status ?? "New",
                Priority = dto.Priority ?? "Medium",
                ExpectedValue = dto.ExpectedValue,
                CreatedDate = DateTime.UtcNow
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create (API)", "Lead", lead.LeadId.ToString(), null, $"Lead: {lead.LeadName}", "Created lead via REST API");

            var resultDto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate
            };

            return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, resultDto);
        }
    }
}
