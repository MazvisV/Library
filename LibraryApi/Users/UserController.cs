using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LibraryApi.Controllers;
using LibraryApi.Persistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public async Task<IActionResult> RegisterUser([FromBody]UserInfo user)
        {
            // Check if non existing
            if (_db.Users.Any(u => u.BNumber == user.BNumber))
            {
                return Conflict();
            }

            await _userManager.AddPhotoToFacialRec(user.Name, user.Image);

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
        [HttpPost("authorize/file")]
        public async Task<IActionResult> Login(IFormFile image)
        {
            // Get name from image reccognition

            string base64ImageRepresentation = null;
            using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                base64ImageRepresentation = Convert.ToBase64String(ms.ToArray());
            }

            return await Login(new AuthRequest
            {
                Image = base64ImageRepresentation
            });
        }

        /// <summary>
        /// Authorize a user
        /// </summary>
        /// <param name="image"></param>
        /// <returns>User info with auth token</returns>
        [HttpPost("authorize")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            // Get name from image reccognition
            var prediction = await _userManager.CheckUserPhoto(request.Image);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == prediction.ClassName);
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

        /// <summary>
        /// Authorize a user
        /// </summary>
        /// <param name="image"></param>
        /// <returns>User info with auth token</returns>
        [Authorize]
        [HttpPost("authorize/confirmation")]
        public async Task<IActionResult> Confirm()
        {
            // Send pic to add to learning algorithm

            var identity = User?.Identity as ClaimsIdentity;
            var userId = int.Parse(identity?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            var tokenString = GetJwtToken(user);

            return Ok(
                new
                {
                    Token = tokenString
                });
        }

        /// <summary>
        /// Authorize a user
        /// </summary>
        /// <param name="image"></param>
        /// <returns>User info with auth token</returns>
        [Authorize]
        [HttpPost("authorize/confirmation/learning")]
        public async Task<IActionResult> Confirm([FromBody] AuthRequest request)
        {
            // Send pic to add to learning algorithm

            var identity = User?.Identity as ClaimsIdentity;
            var userId = int.Parse(identity?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.AddPhotoToFacialRec(user.Name, request.Image);

            var tokenString = GetJwtToken(user);

            return Ok(
                new
                {
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

    public class AuthRequest
    {
        public string Image { get; set; }
    }
}