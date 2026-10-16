using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/followups")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FollowUpsApiController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] string? status)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            var list = await query.OrderBy(f => f.FollowUpDate).Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : null
            }).ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new { success = false, message = "Follow-up date cannot be earlier than today." });
            }

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType,
                Subject = dto.Subject.Trim(),
                Remarks = dto.Remarks,
                Status = dto.Status ?? "Planned",
                CreatedDate = DateTime.UtcNow
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create (API)", "FollowUp", followUp.FollowUpId.ToString(), null, $"Subject: {followUp.Subject}", "Created follow-up via REST API");

            return Ok(new { success = true, followUpId = followUp.FollowUpId, message = "Follow-up scheduled successfully" });
        }
    }
}
