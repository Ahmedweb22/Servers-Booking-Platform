using Microsoft.IdentityModel.Tokens;
namespace Shatbly.Utilities.Dbintializes
{
    public class Dbintialize : IDbintialize
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public Dbintialize(RoleManager<IdentityRole> roleManager ,UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }
    public async Task Intializer()
        {
            if(_context.Database.GetPendingMigrations().Any())
            {
                _context.Database.Migrate();
            }
            if (!_roleManager.Roles.Any())
            {
                await _roleManager.CreateAsync(new(SD.ROLE_SUPER_ADMIN));
                await _roleManager.CreateAsync(new(SD.ROLE_ADMIN));
                await _roleManager.CreateAsync(new(SD.ROLE_WORKER));
                await _roleManager.CreateAsync(new(SD.ROLE_CUSTOMER));

                await _userManager.CreateAsync(new()
                {
                    FName = "Super",
                    LName = "Admin",
                    Name = "Super Admin",
                    Email = "SuperAdmin@gmail.com",
                    EmailConfirmed = true,
                    Phone = "01222222222",
                    UserName = "SuperAdmin"
                }, "Super123@");
                await _userManager.CreateAsync(new()
                {
                    FName = "Admin",
                    LName = "1",
                    Name = "Admin 1",
                    Email = "Admin@gmail.com",
                    EmailConfirmed = true,
                    Phone = "01555555555",  
                    UserName = "Admin"
                }, "Admin123@");
                await _userManager.CreateAsync(new()
                {
                    FName = "Worker",
                    LName = "1",
                    Name = "Worker 1",
                    Email = "Worker@gmail.com",
                    EmailConfirmed = true,
                    Phone = "01111111111",
                    UserName = "Worker"
                }, "Worker123@");
                await _userManager.CreateAsync(new()
                {
                    FName = "Customer",
                    LName = "1",
                    Name = "Customer 1",
                    Email = "Customer@gmail.com",
                    EmailConfirmed = true,
                    Phone = "01000000000",
                    UserName = "Customer"
                },"Customer123@");
                var user = await _userManager.FindByNameAsync("SuperAdmin");
                var user2 = await _userManager.FindByNameAsync("Admin");
                var user3 = await _userManager.FindByNameAsync("Worker");
                var user4 = await _userManager.FindByNameAsync("Customer");
                if (user is not null && user2 is not null&& user3 is not null && user4 is not null) 
                {
                    await _userManager.AddToRoleAsync(user , SD.ROLE_SUPER_ADMIN);
                    await _userManager.AddToRoleAsync(user2 , SD.ROLE_ADMIN);
                    await _userManager.AddToRoleAsync(user3 , SD.ROLE_WORKER);
                    await _userManager.AddToRoleAsync(user4 , SD.ROLE_CUSTOMER);

                    // Add seed addresses and more workers
                    var worker2 = new User
                    {
                        FName = "محمد",
                        LName = "أحمد",
                        Email = "worker2@gmail.com",
                        EmailConfirmed = true,
                        Phone = "01234567891",
                        UserName = "worker2"
                    };
                    await _userManager.CreateAsync(worker2, "Worker123@");
                    await _userManager.AddToRoleAsync(worker2, SD.ROLE_WORKER);

                    var worker3 = new User
                    {
                        FName = "محمود",
                        LName = "علي",
                        Email = "worker3@gmail.com",
                        EmailConfirmed = true,
                        Phone = "01234567892",
                        UserName = "worker3"
                    };
                    await _userManager.CreateAsync(worker3, "Worker123@");
                    await _userManager.AddToRoleAsync(worker3, SD.ROLE_WORKER);

                    _context.Addresses.AddRange(
                        new Address { City = "الإسكندرية", District = "سموحة", Street = "شارع فوزي معاذ", UserId = user3.Id, IsDefault = true, Lat = 31.224, Lng = 29.955 },
                        new Address { City = "القاهرة", District = "المعادي", Street = "شارع 9", UserId = worker2.Id, IsDefault = true, Lat = 29.960, Lng = 31.256 },
                        new Address { City = "الجيزة", District = "الدقي", Street = "شارع التحرير", UserId = worker3.Id, IsDefault = true, Lat = 30.033, Lng = 31.213 },
                        new Address { City = "القاهرة", District = "التجمع الخامس", Street = "شارع التسعين", UserId = user4.Id, IsDefault = true, Lat = 30.005, Lng = 31.450 }
                    );
                    
                    // Create worker profiles
                    var workerUsers = new[] { user3, worker2, worker3 };
                    foreach (var wUser in workerUsers)
                    {
                        if (!_context.WorkerProfiles.Any(wp => wp.UserId == wUser.Id))
                        {
                            _context.WorkerProfiles.Add(new WorkerProfile
                            {
                                UserId = wUser.Id,
                                Bio = $"فني متخصص بخبرة طويلة في منطقته",
                                IsApproved = true,
                                IsAvailable = true,
                                IsVerified = true,
                                RatingAvg = 4.5m,
                                RatingCount = 10,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }

            // Seed addresses and worker profiles if they don't exist
            if (!_context.Addresses.Any())
            {
                var user3 = await _userManager.FindByNameAsync("Worker");
                var user4 = await _userManager.FindByNameAsync("Customer");

                if (user3 != null && user4 != null)
                {
                    // Add more workers if they don't exist
                    var worker2 = await _userManager.FindByNameAsync("worker2");
                    if (worker2 == null)
                    {
                        worker2 = new User
                        {
                            FName = "محمد",
                            LName = "أحمد",
                            Name = "محمد أحمد",
                            Email = "worker2@gmail.com",
                            EmailConfirmed = true,
                            Phone = "01234567891",
                            UserName = "worker2"
                        };
                        await _userManager.CreateAsync(worker2, "Worker123@");
                        await _userManager.AddToRoleAsync(worker2, SD.ROLE_WORKER);
                    }

                    var worker3 = await _userManager.FindByNameAsync("worker3");
                    if (worker3 == null)
                    {
                        worker3 = new User
                        {
                            FName = "محمود",
                            LName = "علي",
                            Name = "محمود علي",
                            Email = "worker3@gmail.com",
                            EmailConfirmed = true,
                            Phone = "01234567892",
                            UserName = "worker3"
                        };
                        await _userManager.CreateAsync(worker3, "Worker123@");
                        await _userManager.AddToRoleAsync(worker3, SD.ROLE_WORKER);
                    }

                    _context.Addresses.AddRange(
                        new Address { City = "الإسكندرية", District = "سموحة", Street = "شارع فوزي معاذ", UserId = user3.Id, IsDefault = true, Lat = 31.224, Lng = 29.955 },
                        new Address { City = "القاهرة", District = "المعادي", Street = "شارع 9", UserId = worker2.Id, IsDefault = true, Lat = 29.960, Lng = 31.256 },
                        new Address { City = "الجيزة", District = "الدقي", Street = "شارع التحرير", UserId = worker3.Id, IsDefault = true, Lat = 30.033, Lng = 31.213 },
                        new Address { City = "القاهرة", District = "التجمع الخامس", Street = "شارع التسعين", UserId = user4.Id, IsDefault = true, Lat = 30.005, Lng = 31.450 }
                    );

                    // Create worker profiles
                    var workerUsers = new[] { user3, worker2, worker3 };
                    foreach (var wUser in workerUsers)
                    {
                        if (!_context.WorkerProfiles.Any(wp => wp.UserId == wUser.Id))
                        {
                            _context.WorkerProfiles.Add(new WorkerProfile
                            {
                                UserId = wUser.Id,
                                Bio = $"فني متخصص بخبرة طويلة في منطقته",
                                IsApproved = true,
                                IsAvailable = true,
                                IsVerified = true,
                                RatingAvg = 4.5m,
                                RatingCount = 10,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }

            // Ensure ALL workers have at least one address
            var allWorkers = _context.WorkerProfiles.Include(w => w.User).ThenInclude(u => u.Addresses).ToList();
            bool changesMade = false;
            foreach (var worker in allWorkers)
            {
                if (worker.User != null && (worker.User.Addresses == null || !worker.User.Addresses.Any()))
                {
                    _context.Addresses.Add(new Address
                    {
                        City = "القاهرة",
                        District = "وسط البلد",
                        Street = "شارع طلعت حرب",
                        UserId = worker.UserId,
                        IsDefault = true
                    });
                    changesMade = true;
                }
            }
            if (changesMade)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
