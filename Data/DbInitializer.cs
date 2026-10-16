using AcxiomCRM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = new[] { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            var usersToCreate = new[]
            {
                (Email: "admin@acxiomcrm.com", Name: "System Administrator", Role: "Admin"),
                (Email: "manager@acxiomcrm.com", Name: "Sales Manager", Role: "Manager"),
                (Email: "sales@acxiomcrm.com", Name: "Yash Sharma (Sales)", Role: "SalesExecutive"),
                (Email: "sales2@acxiomcrm.com", Name: "Rahul Verma (Sales)", Role: "SalesExecutive")
            };

            var userMap = new Dictionary<string, ApplicationUser>();

            foreach (var item in usersToCreate)
            {
                var user = await userManager.FindByEmailAsync(item.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = item.Email,
                        Email = item.Email,
                        EmailConfirmed = true,
                        FullName = item.Name,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(user, "Admin@123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, item.Role);
                    }
                }
                userMap[item.Email] = user;
            }

            // 3. Seed Customers if empty
            if (!await context.Customers.AnyAsync())
            {
                var salesUser = userMap["sales@acxiomcrm.com"];
                var sales2User = userMap["sales2@acxiomcrm.com"];

                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-0001",
                        CustomerName = "Tata Consultancy Services",
                        Email = "procurement@tcs.com",
                        Phone = "9876543210",
                        CompanyName = "TCS Ltd",
                        Address = "Gateway Park, Road No 13",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        CreatedBy = salesUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-0002",
                        CustomerName = "Infosys BPM Solutions",
                        Email = "vendor@infosys.com",
                        Phone = "9812345678",
                        CompanyName = "Infosys Ltd",
                        Address = "Electronics City Phase 1",
                        City = "Bengaluru",
                        State = "Karnataka",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        CreatedBy = sales2User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-0003",
                        CustomerName = "Reliance Digital Enterprise",
                        Email = "contact@reliancedigital.in",
                        Phone = "9823456789",
                        CompanyName = "Reliance Industries",
                        Address = "BKC Complex, Bandra East",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        CreatedBy = salesUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-0004",
                        CustomerName = "Wipro Technologies",
                        Email = "enterprise@wipro.com",
                        Phone = "9845671234",
                        CompanyName = "Wipro Global",
                        Address = "Sarjapur Road, Kaikondrahalli",
                        City = "Bengaluru",
                        State = "Karnataka",
                        Status = "Inactive",
                        CreatedDate = DateTime.UtcNow.AddDays(-60),
                        CreatedBy = salesUser.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // 4. Seed Leads if empty
            if (!await context.Leads.AnyAsync())
            {
                var salesUser = userMap["sales@acxiomcrm.com"];
                var sales2User = userMap["sales2@acxiomcrm.com"];

                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-0001",
                        LeadName = "HDFC FinTech Division",
                        Email = "partnerships@hdfcbank.com",
                        Phone = "9898989801",
                        CompanyName = "HDFC Bank",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 450000,
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedTo = salesUser.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-0002",
                        LeadName = "Mahindra Auto Cloud",
                        Email = "techlead@mahindra.com",
                        Phone = "9898989802",
                        CompanyName = "Mahindra & Mahindra",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "High",
                        ExpectedValue = 650000,
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        AssignedTo = sales2User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-0003",
                        LeadName = "Zomato Logistics Corp",
                        Email = "supplies@zomato.in",
                        Phone = "9898989803",
                        CompanyName = "Eternal Ltd",
                        Source = "Advertisement",
                        Status = "New",
                        Priority = "Medium",
                        ExpectedValue = 200000,
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        AssignedTo = salesUser.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-0004",
                        LeadName = "Apollo Health Systems",
                        Email = "it@apollohospitals.com",
                        Phone = "9898989804",
                        CompanyName = "Apollo Healthcare",
                        Source = "Partner",
                        Status = "Lost",
                        Priority = "Low",
                        ExpectedValue = 150000,
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedTo = salesUser.Id
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities if empty
            if (!await context.Opportunities.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0001");
                var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0002");
                var cust3 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0003");
                var salesUser = userMap["sales@acxiomcrm.com"];

                var opportunities = new List<Opportunity>
                {
                    new Opportunity
                    {
                        OpportunityName = "Cloud Migration Services Deal",
                        CustomerId = cust1?.CustomerId,
                        Amount = 750000,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-14),
                        AssignedTo = salesUser.Id,
                        Notes = "Final price discount negotiations ongoing."
                    },
                    new Opportunity
                    {
                        OpportunityName = "ERP Integration Enterprise Suite",
                        CustomerId = cust2?.CustomerId,
                        Amount = 1200000,
                        Stage = "Proposal",
                        Probability = 50,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedTo = salesUser.Id,
                        Notes = "RFP submitted; technical demo scheduled."
                    },
                    new Opportunity
                    {
                        OpportunityName = "CRM Security Audit Subscription",
                        CustomerId = cust3?.CustomerId,
                        Amount = 350000,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                        Status = "Won",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        AssignedTo = salesUser.Id,
                        Notes = "Deal successfully closed and contract executed."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Data Warehouse Modernization",
                        CustomerId = cust1?.CustomerId,
                        Amount = 500000,
                        Stage = "Qualification",
                        Probability = 25,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(40),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        AssignedTo = salesUser.Id,
                        Notes = "Initial requirement gathering phase."
                    }
                };

                context.Opportunities.AddRange(opportunities);
                await context.SaveChangesAsync();
            }

            // 6. Seed Follow-ups if empty
            if (!await context.FollowUps.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0001");
                var lead1 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-0001");
                var salesUser = userMap["sales@acxiomcrm.com"];

                var followUps = new List<FollowUp>
                {
                    new FollowUp
                    {
                        CustomerId = cust1?.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(1),
                        FollowUpType = "Call",
                        Subject = "Contract review call with VP Tech",
                        Remarks = "Discuss SLA clauses and payment schedule",
                        Status = "Planned",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new FollowUp
                    {
                        LeadId = lead1?.LeadId,
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = "Meeting",
                        Subject = "Product Demo for HDFC Team",
                        Remarks = "Showcase reporting and API security features",
                        Status = "Planned",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new FollowUp
                    {
                        CustomerId = cust1?.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-1),
                        FollowUpType = "Email",
                        Subject = "Send updated commercial quote",
                        Remarks = "Overdue task from yesterday",
                        Status = "Planned",
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-4)
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Initial Audit Logs
            if (!await context.AuditLogs.AnyAsync())
            {
                context.AuditLogs.AddRange(
                    new AuditLog
                    {
                        UserId = userMap["admin@acxiomcrm.com"].Id,
                        UserName = "System Administrator",
                        Action = "Login",
                        EntityName = "Security",
                        RecordId = userMap["admin@acxiomcrm.com"].Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        IpAddress = "127.0.0.1",
                        Details = "Admin initial setup login"
                    },
                    new AuditLog
                    {
                        UserId = userMap["admin@acxiomcrm.com"].Id,
                        UserName = "System Administrator",
                        Action = "Create",
                        EntityName = "User",
                        RecordId = userMap["sales@acxiomcrm.com"].Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        IpAddress = "127.0.0.1",
                        NewValue = "Role: SalesExecutive, User: sales@acxiomcrm.com",
                        Details = "Created SalesExecutive user"
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
