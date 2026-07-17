using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;
using QuestPDF.Infrastructure;
using Shtbly.HealthCheck;
using Shtbly.Hubs;
using Shtbly.Services.AI;
using Shtbly.Services.AvailabilityService;
using Shtbly.Services.BookingSystem;
using Shtbly.Services.Chat;
using Shtbly.Services.CurrentWorkerService1;
using Shtbly.Services.File_Service;
using Shtbly.Services.Hangfire.TestJob;
using Shtbly.Services.Notification;
using Shtbly.Services.Portfolio;
using Shtbly.Services.TokenServices;
using Shtbly.Services.WorkerProfileService;
using Shtbly.UnitOfWork;
using Shtbly.Utilities.Dbintializes;
using Stripe;
using Address = Shtbly.Models.Address;
using Coupon = Shtbly.Models.Coupon;
using FileService = Shtbly.Services.File_Service.FileService;
using LicenseType = QuestPDF.Infrastructure.LicenseType;
using PromotionCode = Shtbly.Models.PromotionCode;
using Review = Shtbly.Models.Review;
using TokenService = Shtbly.Services.TokenServices.TokenService;
namespace Shtbly

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
                .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SQL Server Connection")
                .AddCheck<DatabaseCrudHealthCheck>("Database CRUD Operations")
                .AddCheck<DependencyInjectionHealthCheck>("Dependency Injection & Services")
                .AddCheck<ExternalServicesHealthCheck>("External APIs (Stripe & Groq)")
                .AddCheck<HangfireHealthCheck>("Hangfire Background Processing")
                .AddCheck<WorkerHealthCheck>("Worker Service")
                .AddCheck<CouponHealthChack>("Coupon Repository")
                .AddCheck<BookingHealthChack>("Booking Repository")
                //.AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), name: "SQL Server")
                //.AddCheck<WorkerHealthCheck>("Worker Service")
                //.AddCheck<CouponHealthChack>("Coupon Repository")
                //.AddCheck<BookingHealthChack>("Booking Repository")
                .AddCheck<DatabaseQueriesHealthCheck>("Database Queries")
                .AddCheck<EmailServiceHealthCheck>("Email Service")
                .AddCheck<StripeHealthCheck>("Stripe Integration")
                .AddCheck<GroqAiHealthCheck>("Groq AI Service")
                .AddCheck<SmsServiceHealthCheck>("SMS Service");

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
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOrLocal", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        if (context.Resource is HttpContext httpContext)
                        {
                            if (httpContext.User.Identity?.IsAuthenticated == true &&
                                (httpContext.User.IsInRole(SD.ROLE_ADMIN) || httpContext.User.IsInRole(SD.ROLE_SUPER_ADMIN)))
                            {
                                return true;
                            }

                            var remoteIp = httpContext.Connection.RemoteIpAddress;
                            if (remoteIp != null)
                            {
                                return System.Net.IPAddress.IsLoopback(remoteIp);
                            }
                        }
                        return false;
                    });
                });
            });
            var authenticationBuilder = builder.Services.AddAuthentication();
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authenticationBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;

                    options.ClientSecret = googleClientSecret;

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
            }
            builder.Services.AddScoped<IRepository<OTP_Verification>, Repository<OTP_Verification>>();
            // Add services to the container.
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                options.ModelBinderProviders.Insert(0, new Shtbly.Utilities.EncryptedIdModelBinderProvider());
            })
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
            builder.Services.AddTransient<Shtbly.Utilities.IEmailSenderWithAttachment, Shtbly.Utilities.EmailSender>();
            builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(provider => provider.GetRequiredService<Shtbly.Utilities.IEmailSenderWithAttachment>());
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
            builder.Services.AddScoped<Shtbly.UnitOfWork.IUnitOfWork, Shtbly.UnitOfWork.UnitOfWork>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
            builder.Services.AddScoped<ISmsService, MockSmsService>();
            builder.Services.AddScoped<IBookingNotificationService, BookingNotificationService>();
            builder.Services.AddScoped<Shtbly.UnitOfWork.IUnitOfWork, Shtbly.UnitOfWork.UnitOfWork>();
            builder.Services.AddSignalR();
            //Chat
            builder.Services.AddScoped<Shtbly.Services.Chat.IChatService, ChatService>();
            //builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();

            StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"];
            // Register Chatbot Services
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IChatAiService, GroqChatService>();
            builder.Services.AddScoped<IIdValidationService, IdValidationService>();
            builder.Services.AddScoped<Shtbly.Services.Receipt.IReceiptService, Shtbly.Services.Receipt.ReceiptService>();
            QuestPDF.Settings.License = LicenseType.Community;

            // Add Hangfire services.
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();

            var app = builder.Build();
            var adminOnly = new AuthorizeAttribute { Roles = $"{SD.ROLE_ADMIN},{SD.ROLE_SUPER_ADMIN}" };

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString()
                    }));
                }
            }).RequireAuthorization("AdminOrLocal");
            app.MapHealthChecks("/health-api-json", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).RequireAuthorization("AdminOrLocal");
            app.MapHealthChecksUI(options =>
            {
                options.UIPath = "/health-ui";
            }).RequireAuthorization(adminOnly);
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
            app.UseHangfireDashboard("/Hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
            });
            app.UseHangfireDashboard();
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}",
                constraints: new { id = new Shtbly.Utilities.HashidOutboundParameterTransformer() });

            app.MapControllerRoute(
                name: "default",
                pattern: "{area=Identity}/{controller=Account}/{action=Index}/{id?}",
                constraints: new { id = new Shtbly.Utilities.HashidOutboundParameterTransformer() })
                .WithStaticAssets();
            app.Run();
        }
    }
}

