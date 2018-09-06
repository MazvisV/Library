using System;

namespace LibraryApi.Persistance
{
    public class Lease
    {
        public int Id { get; set; }

        public DateTime TakenAt { get; set; }

        public DateTime? ReturnedAt { get; set; }

        public int UserId { get; set; }

        public int BookId { get; set; }

        public Book Book { get; set; }

        public User User { get; set; }
    }
}
