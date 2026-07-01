using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using QuestPDF.Infrastructure;
using Shatbly.HealthCheck;
using Shatbly.Hubs;
using Shatbly.Services.AI;
using Shatbly.Services.AvailabilityService;
using Shatbly.Services.BookingSystem;
using Shatbly.Services.Chat;
using Shatbly.Services.CurrentWorkerService1;
using Shatbly.Services.File_Service;
using Shatbly.Services.Hangfire.TestJob;
using Shatbly.Services.Notification;
using Shatbly.Services.Portfolio;
using Shatbly.Services.TokenServices;
using Shatbly.Services.WorkerProfileService;
using Shatbly.UnitOfWork;
using Shatbly.Utilities.Dbintializes;
using Stripe;
using System.Security.Claims;
using System.Text;
using Address = Shatbly.Models.Address;
using Coupon = Shatbly.Models.Coupon;
using FileService = Shatbly.Services.File_Service.FileService;
using LicenseType = QuestPDF.Infrastructure.LicenseType;
using PromotionCode = Shatbly.Models.PromotionCode;
using Review = Shatbly.Models.Review;
using TokenService = Shatbly.Services.TokenServices.TokenService;

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
            builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SQL Server")
    .AddCheck<WorkerHealthCheck>("Worker Service")
    .AddCheck<CouponHealthChack>("Coupon Repository")
    .AddCheck<BookingHealthChack>("Booking Repository");

            //hangfire
            builder.Services.AddHangfire(config => config
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();

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
            //        builder.Services.AddAuthentication(opt =>
            //        {
            //            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //        })
            //.AddJwtBearer(options =>
            //{
            //    options.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ClockSkew = TimeSpan.Zero,
            //        ValidateIssuer = true,
            //        ValidIssuer = "https://localhost:7282",
            //        ValidateAudience = true,
            //        ValidAudience = "https://localhost:7282",
            //        ValidateLifetime = true,
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("azrzVS3bami7WdOJh38veSM92OOPJh98BDrqwUakteQ=")),
            //        RoleClaimType = ClaimTypes.Role
            //    };
            //});
            builder.Services
            .AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId =
                    builder.Configuration["Authentication:Google:ClientId"];

                options.ClientSecret =
                    builder.Configuration["Authentication:Google:ClientSecret"];

                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;

                options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
                {
                    OnRemoteFailure = context =>
                    {
                        var failureMessage = context.Failure?.Message ?? "Remote authentication failed.";
                        context.Response.Redirect(context.Request.PathBase + "/Identity/Account/Login?remoteError=" + System.Net.WebUtility.UrlEncode(failureMessage));
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
            builder.Services.AddScoped<IRepository<OTP_Verification>, Repository<OTP_Verification>>();
            // Add services to the container.
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddControllersWithViews()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization();
            builder.Services.AddScoped<IDbintialize, Dbintialize>();
            builder.Services.AddScoped<IRepository<User>, Repository<User>>();
            builder.Services.AddScoped<IRepository<WorkerProfile>, Repository<WorkerProfile>>();
            builder.Services.AddScoped<IRepository<Address>, Repository<Address>>();
            builder.Services.AddScoped<IRepository<Booking>, Repository<Booking>>();
            builder.Services.AddScoped<IRepository<Coupon>, Repository<Coupon>>();
            builder.Services.AddScoped<IRepository<Promotion>, Repository<Promotion>>();
            builder.Services.AddScoped<IRepository<PromotionCode>, Repository<PromotionCode>>();
            builder.Services.AddScoped<IRepository<Banner>, Repository<Banner>>();
            builder.Services.AddScoped<IRepository<WorkerService>, Repository<WorkerService>>();
            builder.Services.AddScoped<IRepository<ServiceCategory>, Repository<ServiceCategory>>();
            builder.Services.AddScoped<IBookingSystemService, BookingSystemService>();
            builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
            builder.Services.AddScoped<IRepository<Notification>, Repository<Notification>>();
            builder.Services.AddScoped<IRepository<Review>, Repository<Review>>();
            builder.Services.AddScoped<IAccountService, Services.AccountService>();
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
            builder.Services.AddScoped<IWorkerProfileService, WorkerProfileService>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IPortfolioService, PortfolioService>();
            builder.Services.AddScoped<IFilePortfolioService, FilePortfolioService>();
            builder.Services.AddTransient<ITokenService, TokenService>();
            builder.Services.AddScoped<ICurrentWorkerService, CurrentWorkerService>();
            builder.Services.AddScoped<IEarningsService, EarningsService>();
            builder.Services.AddScoped<IWithdrawalService, WithdrawalService>();
            //hangfire
            builder.Services.AddScoped<TestJob>();
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
            builder.Services.AddScoped<Shatbly.Services.Chat.IChatService, ChatService>();
            //builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();

            StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"];
            // Register Chatbot Services
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IChatAiService, GroqChatService>();
            builder.Services.AddScoped<IIdValidationService, IdValidationService>();
            QuestPDF.Settings.License = LicenseType.Community;

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHangfireDashboard("/Hangfire");
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
            app.MapHub<TrackingHub>("/trackingHub");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Localization middleware — must be after UseRouting, before UseAuthentication
            var supportedCultures = new[] { "en", "ar" };
            app.UseRequestLocalization(options =>
            {
                options.SetDefaultCulture("en")
                       .AddSupportedCultures(supportedCultures)
                       .AddSupportedUICultures(supportedCultures);
                options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
            });
            var scope = app.Services.CreateScope();
            var Service = scope.ServiceProvider.GetService<IDbintialize>();
            //Service.Intializer();
            Service.Intializer().GetAwaiter().GetResult();
            app.UseAuthentication();
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

