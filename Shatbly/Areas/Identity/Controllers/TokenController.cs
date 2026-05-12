using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shatbly.Services.TokenServices;

namespace Shatbly.Areas.Identity.Controllers
{
   //[Area(SD.IDENTITY_AREA)]
   // public class TokenController : Controller
   // {
   //     private readonly UserManager<User> _userManager;
   //     private readonly ITokenService _tokenService;

   //     public TokenController(UserManager<User> userManager, ITokenService tokenService)
   //     {
   //         _userManager = userManager;
   //         _tokenService = tokenService;
   //     }

   //     [HttpPost]
   //     [Route("refresh")]
   //     public async Task<IActionResult> Refresh(TokenApiModel tokenRequest)
   //     {
   //         if (tokenRequest is null || tokenRequest.RefreshToken is null || tokenRequest.AccessToken is null)
   //         {
   //             return BadRequest("Invalid client request");
   //         }
   //         string accessToken = tokenRequest.AccessToken;
   //         string refreshToken = tokenRequest.RefreshToken;
   //         var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
   //         var username = principal.Identity.Name;
   //         //user
   //         var user = _userManager.Users.FirstOrDefault(e => e.UserName == username);
   //         if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
   //         {
   //             return BadRequest("Invalid client request");
   //         }

   //         var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
   //         var newRefreshToken = _tokenService.GenerateRefreshToken();

   //         user.RefreshToken = newRefreshToken;

   //         await _userManager.UpdateAsync(user);

   //         return Ok(new
   //         {
   //             AccessToken = newAccessToken,
   //             RefreshToken = newRefreshToken
   //         });
   //     }
   //     [HttpPost, Authorize]
   //     [Route("revoke")]
   //     public async Task<IActionResult> Revoke()
   //     {
   //         var username = User.Identity!.Name;
   //         var user = _userManager.Users.FirstOrDefault(e => e.UserName == username);
   //         if (user == null) return BadRequest("Invalid client request");
   //         user.RefreshToken = null;
   //         await _userManager.UpdateAsync(user);
   //         return NoContent();
   //     }
   // }
}
