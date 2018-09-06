using System.Linq;
using System.Threading.Tasks;
using LibraryApi.Persistance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Books
{
    [Route("books")]
    public class BookController : Controller
    {
        private readonly DatabaseContext _db;

        public BookController(DatabaseContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Add a new book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Register(BookInfo book)
        {
            _db.Books.Add(new Book { BarCode = book.QrCode, IsAvailable = true, Title = book.Title });
            await _db.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Get a list of existing books
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var books = await _db.Books.Select(b => new { b.BarCode, b.Title, b.IsAvailable }).ToListAsync();

            return Ok(books);
        }
    }
}