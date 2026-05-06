using Microsoft.AspNetCore.Identity.UI.Services;
using Shatbly.HealthCheck;
using Shatbly.Hubs;
using Shatbly.Services.AvailabilityService;
using Shatbly.Services.BookingSystem;
using Shatbly.Services.Chat;
using Shatbly.Services.CurrentWorkerService1;
using Shatbly.Services.File_Service;
using Shatbly.Services.Notification;
using Shatbly.Services.Portfolio;
using Shatbly.Services.WorkerProfileService;
using Shatbly.UnitOfWork;
using Shatbly.Utilities.Dbintializes;

namespace Shatbly
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            //builder.Services.AddHealthChecks()
            // .AddSqlServer(
            //     builder.Configuration.GetConnectionString("DefaultConnection"),
            //     name: "SQL Server",
            //     failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
            //);
            //builder.Services.AddHealthChecks()
            //.AddCheck<WorkerHealthCheck>("Booking Service")
            //.AddCheck<CouponHealthChack>("Coupon Service");


            builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SQL Server")
    .AddCheck<WorkerHealthCheck>("Worker Service")
    .AddCheck<CouponHealthChack>("Coupon Repository")
    .AddCheck<BookingHealthChack>("Booking Repository"); 

            builder.Services.AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(10); 
                options.MaximumHistoryEntriesPerEndpoint(50);
                options.AddHealthCheckEndpoint("Main API", "/health-api-json");
            })
            .AddInMemoryStorage();

            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            })
               .AddEntityFrameworkStores<ApplicationDbContext>()
               .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });
            builder.Services.AddScoped<IRepository<OTP_Verification>, Repository<OTP_Verification>>();
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IRepository<WorkerProfile> , Repository<WorkerProfile>>();
            builder.Services.AddScoped<IDbintialize, Dbintialize>();
            builder.Services.AddScoped<IRepository<User>, Repository<User>>();
            builder.Services.AddScoped<IRepository<WorkerProfile>, Repository<WorkerProfile>>();
            builder.Services.AddScoped<IRepository<Address>, Repository<Address>>();
            builder.Services.AddScoped<IRepository<Booking>, Repository<Booking>>();
            builder.Services.AddScoped<IRepository<Coupon>, Repository<Coupon>>();
            builder.Services.AddScoped<IRepository<Promotion>, Repository<Promotion>>();    
            builder.Services.AddScoped<IRepository<PromotionCode>, Repository<PromotionCode>>();
            builder.Services.AddScoped<IRepository<Banner>, Repository<Banner>>();
            builder.Services.AddScoped<IRepository<ServiceCategory>, Repository<ServiceCategory>>();
            builder.Services.AddScoped<IRepository<WorkerService>, Repository<WorkerService>>();
            builder.Services.AddScoped<IRepository<ServiceCategory>, Repository<ServiceCategory>>();
            builder.Services.AddScoped<IBookingSystemService, BookingSystemService>();
            builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
            builder.Services.AddScoped<IRepository<Notification>, Repository<Notification>>();
            builder.Services.AddScoped<IAccountService, Services.AccountService>();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
            builder.Services.AddScoped<IWorkerProfileService, WorkerProfileService>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IPortfolioService, PortfolioService>();
            builder.Services.AddScoped<IFilePortfolioService, FilePortfolioService>();
            builder.Services.AddScoped<ICurrentWorkerService, CurrentWorkerService>();
            builder.Services.AddScoped<IEarningsService, EarningsService>();
            builder.Services.AddScoped<IWithdrawalService, WithdrawalService>();
            //Notefication
            builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
            builder.Services.AddScoped<INotificationRepository, SqlNotificationRepository>();
            builder.Services.AddScoped<Shatbly.UnitOfWork.IUnitOfWork, Shatbly.UnitOfWork.UnitOfWork>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddScoped<ISmsService, MockSmsService>();
            builder.Services.AddScoped<IBookingNotificationService, BookingNotificationService>();
            builder.Services.AddScoped<Shatbly.UnitOfWork.IUnitOfWork, Shatbly.UnitOfWork.UnitOfWork>();
            builder.Services.AddSignalR();
            //Chat
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            // 3. ãÓÇÑ ÇáÜ JSON ÇáÈÓíØ (áæ ÍÇÈÈ ÊÞÑÃ ÇáÈíÇäÇÊ ßÜ Text ÚÇÏí Ãæ áÓßÑÈÊ ÎÇÑÌí)
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            error = e.Value.Exception?.Message
                        })
                    });
                    await context.Response.WriteAsync(result);
                }
            });
            app.MapHealthChecks("/health-api-json", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapHealthChecksUI(options =>
            {
                options.UIPath = "/health-ui";
            });
            app.MapHub<NotificationHub>("/hubs/notifications");
            app.MapHub<ChatHub>("/chatHub");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            var scope = app.Services.CreateScope();
            var Service = scope.ServiceProvider.GetService<IDbintialize>();
<<<<<<< HEAD
            Service.Intializer();
=======
            Service.Intializer().GetAwaiter().GetResult();

>>>>>>> 06428f79a9e621ad835e9d7999967080537d3fd6
            app.UseAuthorization();
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=Identity}/{controller=Account}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.Run();
        }
    }
}

