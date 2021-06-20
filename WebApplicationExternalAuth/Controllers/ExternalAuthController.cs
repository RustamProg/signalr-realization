using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplicationExternalAuth.Model;

namespace WebApplicationExternalAuth.Controllers
{
    [ApiController]
    [Route("external-auth")]
    public class ExternalAuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IIdentityServerInteractionService _interaction;

        public ExternalAuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IIdentityServerInteractionService interaction)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _interaction = interaction;
        }

        [Authorize(AuthenticationSchemes = "Google")]
        [HttpGet("google")]
        public IActionResult LoginViaGoogle()
        {
            var callbackUrl = Url.Action("ExternalLoginCallback");
            var props = new AuthenticationProperties
            {
                RedirectUri = callbackUrl,
                Items =
                {
                    { "scheme", "Google" },
                    { "returnUrl", "localhost:5000" }
                }
            };
            return Challenge(props, "Google");
        }

        public async Task<IActionResult> ExternalLoginCallback()
        {
            // Читаем из куки информацию (пока у нас только из гугл куки)
            var result =
                await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error (cookie read)");
            }

            // Берем информацию о пользователе
            var externalUser = result.Principal;
            if (externalUser == null)
            {
                throw new Exception("External authentication error (read user info from cookie)");
            }

            // Берем у пользователя клэймы
            var claims = externalUser.Claims.ToList();
            
            // Далее пытаемся получить его ID (обычно это клэймы subject и NameIdentifier, может быть другое)
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null)
            {
                userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            }

            if (userIdClaim == null)
            {
                throw new Exception("Unknown userId");
            }

            var userUsernameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name) ?? claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname);
            var userEmailClaim = claims.FirstOrDefault(x =>
                x.Type == JwtClaimTypes.Email) ?? claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);

            if (userEmailClaim == null || userUsernameClaim == null)
            {
                throw new Exception("Username or email was not found");
            }
            
            // У нас теперь есть ID юзера из гугл (в данном случае) и имя провадера (гугл)
            var externalUserId = userIdClaim.Value;
            var externalUserUsername = userUsernameClaim.Value;
            var externalUserEmail = userEmailClaim.Value;
            var externalProvider = userIdClaim.Issuer;
            
            // Находим нашего юзера в БД ASP.NET Identity, если такого нет, то создаем нового
            var user = await _userManager.FindByEmailAsync(externalUserEmail);

            if (user == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = externalUserEmail,
                    Email = externalUserEmail,
                    EmailConfirmed = true
                };
                
                var registrationResult = await _userManager.CreateAsync(newUser, "_SuperPass123"); // Костыль одинаковый пароль для всех

                if (!registrationResult.Succeeded)
                {
                    throw new Exception(registrationResult.Errors.First().Description);
                }

                registrationResult =
                    await _userManager.AddClaimsAsync(newUser, new Claim[] {new Claim(JwtClaimTypes.Email, externalUserEmail)});

                if (!registrationResult.Succeeded)
                {
                    throw new Exception(registrationResult.Errors.First().Description);
                }
                
                var roleUser = new IdentityRole {Name = "user"};

                await _userManager.AddToRoleAsync(newUser, roleUser.Name);
                user = await _userManager.FindByEmailAsync(externalUserEmail);
            }

            var sid = result.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            var additionalClaims = new List<Claim>();
            if (sid != null)
            {
                additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            var localSignInProps = new AuthenticationProperties();
            var idToken = result.Properties.GetTokenValue("id_token");
            if (idToken != null)
            {
                localSignInProps.StoreTokens(new []{new AuthenticationToken{Name = "id_token", Value = idToken}});
            }
            
            // Создание куки аутентификации для юзера
            await HttpContext.SignInAsync(new IdentityServerUser(user.Id)
            {
                DisplayName = user.UserName,
                IdentityProvider = externalProvider,
                AdditionalClaims = additionalClaims,
                AuthenticationTime = DateTime.Now
            }, localSignInProps);

            await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

            if (_interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }
    }
}