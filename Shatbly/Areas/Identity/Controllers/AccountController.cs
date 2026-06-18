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
        private readonly IStringLocalizer<AccountController> _localizer;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender, IAccountService accountService,
            IRepository<OTP_Verification> otpRepository, ITokenService tokenService, IStringLocalizer<AccountController> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _accountService = accountService;
            _otpRepository = otpRepository;
            _tokenService = tokenService;
            _localizer = localizer;
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
            TempData["success-notification"] = _localizer["LoggedOutSuccess"].Value;
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


            await _accountService.SendEmailAsync(EmailType.ConfirmEmail, string.Format(_localizer["ConfirmEmailLink"].Value, confirmationLink), applicationUser);

            await _userManager.AddToRoleAsync(applicationUser, SD.ROLE_CUSTOMER);

            TempData["success-notification"] = _localizer["UserCreatedSuccess"].Value;
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
                TempData["success-notification"] = _localizer["EmailConfirmedSuccess"].Value;
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["error-notification"] = _localizer["EmailConfirmError"].Value;
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
                ModelState.AddModelError(string.Empty, _localizer["InvalidLoginAttempt"].Value);
                return View(model);
            }
 
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("EmailOrUserName", _localizer["ConfirmEmailFirst"].Value);
                    return View(model);
                }
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, _localizer["AccountLockedOut"].Value);
                    return View(model);
                }
                ModelState.AddModelError(string.Empty, _localizer["InvalidLoginAttempt"].Value);
                return View(model);
            }
            if (await _userManager.IsInRoleAsync(user, SD.ROLE_WORKER))
            {
                TempData["success-notification"] = string.Format(_localizer["WelcomeBack"].Value, user.UserName);
                return RedirectToAction("Details" , "WorkerProfile", new {area = "Worker"});
            }
            else if (await _userManager.IsInRoleAsync(user, SD.ROLE_CUSTOMER))
            {
                TempData["success-notification"] = string.Format(_localizer["WelcomeBack"].Value, user.UserName);
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

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
                await _accountService.SendEmailAsync(EmailType.ResendConfirmationEmail, string.Format(_localizer["ConfirmEmailLink"].Value, confirmationLink), user);
            }

            TempData["success-notification"] = _localizer["ResendConfirmationSent"].Value;
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
                TempData["error-notification"] = _localizer["ConfirmEmailBeforeReset"].Value;
                return RedirectToAction(" ResendEmailConfirmation");
            }
            if (user is not null && userOtpsCount < 5)
            {
                string otp = new Random().Next(1000, 9999).ToString();
                string msg = string.Format(_localizer["OtpEmailBody"].Value, otp);
                await _accountService.SendEmailAsync(EmailType.ForgetPassword, msg, user);
                await _otpRepository.CreateAsync(new()
                {
                    UserId = user.Id,
                    Code = otp,
                });
                await _otpRepository.CommitAsync();
                TempData["success-notification"] = _localizer["OtpSentSuccess"].Value;
            }
            else if (userOtpsCount >= 5)
            {
                TempData["error-notification"] = _localizer["OtpLimitExceeded"].Value;
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
                ModelState.AddModelError(string.Empty, _localizer["InvalidOtp"].Value);
                return View(model);
            }

            var otp = (await _otpRepository.GetAsync()).Where(e => e.UserId == user.Id && !e.IsUsed).OrderBy(e => e.Id).LastOrDefault();
            if (otp == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["InvalidOtp"].Value);
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
                ModelState.AddModelError(string.Empty, _localizer["UserNotFound"].Value);
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
            TempData["success-notification"] = _localizer["PasswordResetSuccess"].Value;
            return RedirectToAction("Login");
        }
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
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
                ModelState.AddModelError(string.Empty, string.Format(_localizer["ExternalProviderError"].Value, remoteError));
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["EmailClaimNotReceived"].Value);
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
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

            var existingLogins = await _userManager.GetLoginsAsync(user);
            if (!existingLogins.Any(x => x.LoginProvider == info.LoginProvider))
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }
    }

}
