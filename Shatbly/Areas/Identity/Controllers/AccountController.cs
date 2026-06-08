using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Shatbly.Services;
using Shatbly.Services.TokenServices;
using Shatbly.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Shatbly.Areas.Identity.Controllers
{
    [Area(SD.IDENTITY_AREA)]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IAccountService _accountService;
        private readonly IRepository<OTP_Verification> _otpRepository;
        private readonly ITokenService _tokenService;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender, IAccountService accountService,
            IRepository<OTP_Verification> otpRepository,ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _accountService = accountService;
            _otpRepository = otpRepository;
            _tokenService = tokenService;
        }
        public IActionResult Index()
        { 
        return  View();
        }
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["success-notification"] = "Logged out successfully.";
            return RedirectToAction("Login", "Account", new { area = "Identity" });

        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            User applicationUser = new()
            {
                UserName = model.UserName,
                Email = model.Email,
               FName = model.FName,
               LName = model.LName,
               Phone = model.Phone
           };
            var result = await _userManager.CreateAsync(applicationUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = applicationUser.Id, token = token }, Request.Scheme);


            await _accountService.SendEmailAsync(EmailType.ConfirmEmail, $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Click here to confirm your account</a>", applicationUser);

            await _userManager.AddToRoleAsync(applicationUser, SD.ROLE_CUSTOMER);

            TempData["success-notification"] = "User created successfully";
            return RedirectToAction("Login");
        }
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Register", "Account", new { area = "Identity" });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["success-notification"] = "Email confirmed successfully. You can now log in.";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["error-notification"] = "Error confirming your email. Please try again.";
            }
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.EmailOrUserName) ??
               await _userManager.FindByNameAsync(model.EmailOrUserName);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
 
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("EmailOrUserName", "Confirm your email before logging in.");
                    return View(model);
                }
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Your account is locked out. Please try again later.");
                    return View(model);
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
            if (await _userManager.IsInRoleAsync(user, SD.ROLE_WORKER))
            {
                TempData["success-notification"] = $"Welcome back {user.UserName}!";
                return RedirectToAction("Details" , "WorkerProfile", new {area = "Worker"});
            }
            else if (await _userManager.IsInRoleAsync(user, SD.ROLE_CUSTOMER))
            {
                TempData["success-notification"] = $"Welcome back {user.UserName}!";
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }
        //    var Claims = new List<Claim>();
        //    {
        //        Claims.Add(new(ClaimTypes.NameIdentifier, user.Id));
        //        Claims.Add(new(ClaimTypes.Name, user.UserName!));
        //        Claims.Add(new(ClaimTypes.Email, user.Email!));
        //        Claims.Add(new(JwtRegisteredClaimNames.Sub, user.Id));
        //        Claims.Add(new Claim(
        //JwtRegisteredClaimNames.Iat,
        //new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
        //ClaimValueTypes.Integer64));
        //    }

        //    var userRoles = await _userManager.GetRolesAsync(user);
        //    foreach (var item in userRoles)
        //    {
        //        Claims.Add(new Claim(ClaimTypes.Role, item));
        //    }
        //    var accesstoken = _tokenService.GenerateAccessToken(Claims);
        //    var refreshToken = _tokenService.GenerateRefreshToken();

        //    user.RefreshToken = refreshToken;
        //    user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);

        //    await _userManager.UpdateAsync(user);

            return RedirectToAction("Index", "Home" , new { area = "Admin" });
        }
        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = await _userManager.FindByEmailAsync(model.EmailOrUserName) ??
                       await _userManager.FindByNameAsync(model.EmailOrUserName);
            if (user is not null && !user.EmailConfirmed)
            {

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ResendConfirmationEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);
                await _accountService.SendEmailAsync(EmailType.ResendConfirmationEmail, $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Click here to confirm your account</a>", user);
            }

            TempData["success-notification"] = "If an account with that email or username exists and is not confirmed, a confirmation email has been resent.";
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.EmailOrUserName) ??
                       await _userManager.FindByNameAsync(model.EmailOrUserName);
            var userOtpsCount = (await _otpRepository.GetAsync(e => user.Id == e.UserId && e.CreatedAt >= DateTime.UtcNow.AddHours(-24))).Count();
            if (!user.EmailConfirmed)
            {
                TempData["error-notification"] = "Please confirm your email before resetting your password.";
                return RedirectToAction(" ResendEmailConfirmation");
            }
            if (user is not null && userOtpsCount < 5)
            {
                string otp = new Random().Next(1000, 9999).ToString();
                string msg = $"<h1>Your OTP for password reset is: {otp}. Don't share it</h1>";
                await _accountService.SendEmailAsync(EmailType.ForgetPassword, msg, user);
                await _otpRepository.CreateAsync(new()
                {
                    UserId = user.Id,
                    Code = otp,
                });
                await _otpRepository.CommitAsync();
                TempData["success-notification"] = "Send OTP to your email Successfully.";
            }
            else if (userOtpsCount >= 5)
            {
                TempData["error-notification"] = "You have exceeded the maximum number of OTP requests. Please try again later.";
                return RedirectToAction("ForgetPassword");
            }

            return RedirectToAction("ValidateOTP", new { applicationUserId = user.Id });
        }
        [HttpGet]
        public IActionResult ValidateOTP(string applicationUserId)
        {
            var model = new ValidateOTPVM
            {
                UserId = applicationUserId
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ValidateOTP(ValidateOTPVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid OTP.");
                return View(model);
            }

            var otp = (await _otpRepository.GetAsync()).Where(e => e.UserId == user.Id && !e.IsUsed).OrderBy(e => e.Id).LastOrDefault();
            if (otp == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid OTP.");
                return View(model);
            }
            otp.IsUsed = true;
            return RedirectToAction("ResetPassword", new { userId = user.Id });
        }
        [HttpGet]
        public IActionResult ResetPassword(string userId)
        {
            var model = new ResetPasswordVM
            {
                UserId = userId
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            TempData["success-notification"] = "Your password has been reset successfully. You can now log in with your new password.";
            return RedirectToAction("Login");
        }
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // ✅ validate الـ returnUrl الجاي من الـ form
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Customer/Home/Index";

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl = safeReturnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return RedirectToAction(nameof(Login));
            }

            // 1. الحصول على بيانات التسجيل من المزود الخارجي (جوجل مثلاً)
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // 2. محاولة تسجيل الدخول باستخدام بيانات المزود الخارجي
            // لو اليوزر سجل قبل كده وتم ربط حسابه، السطر ده هيدخله علطول
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            // 3. لو السطر اللي فوق فشل، ده معناه إن اليوزر أول مرة يسجل أو حسابه مش مربوط
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                ModelState.AddModelError(string.Empty, "Email claim not received from provider.");
                return RedirectToAction(nameof(Login));
            }

            // ابحث عن اليوزر في قاعدة البيانات بالإيميل
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // إنشاء مستخدم جديد لو مش موجود
                var username = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
                Random random = new Random();

                user = new User
                {
                    UserName = username.Replace(" ", "") + random.Next(1000, 9999),
                    Email = email,
                    EmailConfirmed = true
                };

                var createUserResult = await _userManager.CreateAsync(user);
                if (!createUserResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }
            }

            // 4. ربط الحساب الخارجي بالحساب المحلي (لو مش مربوطين)
            var existingLogins = await _userManager.GetLoginsAsync(user);
            if (!existingLogins.Any(x => x.LoginProvider == info.LoginProvider))
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }
            }

            // 5. أهم خطوة: تسجيل الدخول الفعلي بعد الإنشاء أو الربط
            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

            // ✅ كده
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }

}
