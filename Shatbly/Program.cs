using Microsoft.AspNetCore.Identity.UI.Services;
using Shatbly.Services.AvailabilityService;
using Shatbly.Services.BookingSystem;
using Shatbly.Services.CurrentWorkerService1;
using Shatbly.Services.File_Service;
using Shatbly.Services.Portfolio;
using Shatbly.Services.WithdrawalService;
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
            builder.Services.AddScoped<Shatbly.UnitOfWork.IUnitOfWork, Shatbly.UnitOfWork.UnitOfWork>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            var scope = app.Services.CreateScope();
            var Service = scope.ServiceProvider.GetService<IDbintialize>();
            Service.Intializer();

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
