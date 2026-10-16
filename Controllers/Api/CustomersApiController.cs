using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public CustomersApiController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        // GET: api/customers
        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? search)
        {
            var query = _context.Customers.Include(c => c.Owner).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CustomerName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term) ||
                    c.Phone.Contains(term) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
            }

            var list = await query.OrderByDescending(c => c.CreatedDate).Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerName = c.Owner != null ? c.Owner.FullName : null
            }).ToListAsync();

            return Ok(list);
        }

        // GET: api/customers/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var customer = await _context.Customers.Include(c => c.Owner).FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound(new { success = false, message = $"Customer with ID {id} not found." });
            }

            var dto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                OwnerName = customer.Owner?.FullName
            };

            return Ok(dto);
        }

        // POST: api/customers
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            // Duplicate check
            if (await _crmService.IsCustomerDuplicateAsync(dto.Email, dto.Phone))
            {
                return Conflict(new { success = false, message = "Customer with this email or phone number already exists." });
            }

            var customerCode = await _crmService.GenerateCustomerCodeAsync();
            var customer = new Customer
            {
                CustomerCode = customerCode,
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = dto.Status ?? "Active",
                CreatedDate = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create (API)", "Customer", customer.CustomerId.ToString(), null, $"Customer: {customer.CustomerName}", "Created customer via REST API");

            var resultDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
        }

        // PUT: api/customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { success = false, message = $"Customer with ID {id} not found." });
            }

            if (await _crmService.IsCustomerDuplicateAsync(dto.Email, dto.Phone, id))
            {
                return Conflict(new { success = false, message = "Another customer already has this email or phone." });
            }

            var oldValue = $"Name: {customer.CustomerName}, Email: {customer.Email}";

            customer.CustomerName = dto.CustomerName.Trim();
            customer.Email = dto.Email.Trim().ToLower();
            customer.Phone = dto.Phone.Trim();
            customer.CompanyName = dto.CompanyName?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.City = dto.City?.Trim();
            customer.State = dto.State?.Trim();
            customer.Status = dto.Status ?? customer.Status;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Update (API)", "Customer", id.ToString(), oldValue, $"Name: {customer.CustomerName}, Email: {customer.Email}", "Updated via REST API");

            return Ok(new { success = true, message = "Customer updated successfully" });
        }

        // DELETE: api/customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { success = false, message = $"Customer with ID {id} not found." });
            }

            customer.Status = "Inactive";
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Deactivate (API)", "Customer", id.ToString(), "Status: Active", "Status: Inactive", "Customer deactivated via REST API");

            return Ok(new { success = true, message = $"Customer with ID {id} deactivated successfully" });
        }
    }
}
