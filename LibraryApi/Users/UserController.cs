using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LibraryApi.Controllers;
using LibraryApi.Persistance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LibraryApi.Users
{
    [Route("users")]
    public class UserController : Controller
    {
        private readonly DatabaseContext _db;
        private readonly UserManager _userManager;

        public UserController(DatabaseContext db, UserManager userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RegisterUser(UserInfo user)
        {
            // Check if non existing
            if (_db.Users.Any(u => u.BNumber == user.BNumber))
            {
                return Conflict();
            }

            // Register in recognition
            _db.Users.Add(
                new User
                {
                    BNumber = user.BNumber,
                    Email = user.Email,
                    Image = user.Image,
                    Name = user.Name
                });

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Authorize a user
        /// </summary>
        /// <param name="image"></param>
        /// <returns>User info with auth token</returns>
        [HttpPost("authorize")]
        public async Task<IActionResult> Login([FromBody] string image)
        {
            // Get name from image reccognition
            var name = await _userManager.CheckUserPhoto(image);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
            {
                return NotFound();
            }

            var tokenString = GetJwtToken(user);

            return Ok(
                new UserWithToken
                {
                    BNumber = user.BNumber,
                    Email = user.Email,
                    Image = user.Image,
                    Name = user.Name,
                    Token = tokenString
                });
        }

        private string GetJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("myhugesecretkey123456789012345678901234567890qwertyuio");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new[]
                        {
                           new Claim(ClaimTypes.Name, user.Id.ToString()) 
                        }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}