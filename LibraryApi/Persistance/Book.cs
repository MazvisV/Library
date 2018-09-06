using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApi.Persistance
{
    public class Book
    {
        public int Id { get; set; }

        public string BarCode { get; set; }

        public string Title { get; set; }

        public bool IsAvailable { get; set; }

        public Lease Lease { get; set; }
    }
}
