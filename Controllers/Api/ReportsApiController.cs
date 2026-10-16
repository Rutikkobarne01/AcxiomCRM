using AcxiomCRM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reports/pipeline
        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport()
        {
            var opportunities = await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedUser)
                .ToListAsync();

            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var byStage = stages.Select(s =>
            {
                var match = opportunities.Where(o => o.Stage == s).ToList();
                return new
                {
                    Stage = s,
                    Count = match.Count,
                    TotalAmount = match.Sum(m => m.Amount),
                    WeightedAmount = match.Sum(m => (m.Amount * m.Probability) / 100m)
                };
            }).ToList();

            var totalPipeline = opportunities.Where(o => o.Status == "Open").Sum(o => o.Amount);
            var totalWeighted = opportunities.Where(o => o.Status == "Open").Sum(o => (o.Amount * o.Probability) / 100m);

            return Ok(new
            {
                TotalOpportunities = opportunities.Count,
                TotalPipelineValue = totalPipeline,
                TotalWeightedValue = totalWeighted,
                Stages = byStage
            });
        }
    }
}
