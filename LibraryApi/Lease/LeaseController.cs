using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LibraryApi.Persistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Lease
{
    [Authorize]
    [Route("Lease")]
    public class LeaseController : Controller
    {
        private readonly DatabaseContext _db;

        public LeaseController(DatabaseContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get a list of leases
        /// </summary>
        /// <param name="bookImage"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var identity = User?.Identity as ClaimsIdentity;
            var userId = int.Parse(identity?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);

            var leases = await _db.Leases
                .Where(u => u.UserId == userId
                && u.ReturnedAt == null)
                .Select(
                    a => new
                    {
                        a.Book.Title,
                        a.TakenAt
                    })
                .ToListAsync();

            return Ok(leases);
        }


        /// <summary>
        /// Lease a book
        /// </summary>
        /// <param name="bookImage"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Lease([FromBody] Lease leaseRequest)
        {
            var identity = User?.Identity as ClaimsIdentity;
            var userId = int.Parse(identity?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);

            var user = await _db.Users
                .Include(u => u.Leases)
                .SingleAsync(u => u.Id == userId);

            // get book barcode
            var book = await _db.Books.Where(b => b.BarCode == leaseRequest.BarCode).FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound();
            }

            if (!book.IsAvailable)
            {
                return Conflict();
            }

            if (user.Leases.Count > 5)
            {
                return BadRequest("error.more_that_5_books");
            }

            book.IsAvailable = false;

            var lease = new Persistance.Lease
            {
                Book = book,
                TakenAt = DateTime.UtcNow,
                User = user
            };
            _db.Leases.Add(lease);

            await _db.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Return a book
        /// </summary>
        /// <param name="bookImage"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<IActionResult> Return([FromBody] Lease leaseRequest)
        {
            var identity = User?.Identity as ClaimsIdentity;
            var userId = int.Parse(identity?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value);

            var lease = await _db.Leases
                .Where(
                    l => l.Book.BarCode == leaseRequest.BarCode
                        && l.UserId == userId)
                .FirstOrDefaultAsync();

            // get book barcode
            var book = await _db.Books.Where(b => b.BarCode == leaseRequest.BarCode).FirstOrDefaultAsync();

            if (lease == null)
            {
                return NotFound();
            }

            book.IsAvailable = true;
            lease.ReturnedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok();
        }
    }

    public class Lease
    {
        public string BarCode { get; set; }
    }
}