using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplicationExternalAuth.Entities;
using WebApplicationExternalAuth.Model;

namespace WebApplicationExternalAuth.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDto user)
        {
            var newUser = new ApplicationUser
            {
                UserName = user.Username,
                Email = user.Email,
                EmailConfirmed = true
            };

            var registrationResult = await _userManager.CreateAsync(newUser, user.Password);

            if (!registrationResult.Succeeded)
            {
                return BadRequest(registrationResult.Errors.First().Description);
            }

            registrationResult =
                await _userManager.AddClaimsAsync(newUser, new Claim[] {new Claim(JwtClaimTypes.Email, user.Email)});

            if (!registrationResult.Succeeded)
            {
                return BadRequest(registrationResult.Errors.First().Description);
            }

            var roleAdmin = new IdentityRole {Name = "admin"};
            var roleUser = new IdentityRole {Name = "user"};

            try
            {
                await _roleManager.CreateAsync(roleAdmin);
                await _roleManager.CreateAsync(roleUser);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            await _userManager.AddToRoleAsync(newUser, user.RoleName == "admin" ? roleAdmin.Name : roleUser.Name);

            return Ok(newUser);
        }
    }
}