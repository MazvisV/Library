using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LibraryApi.Persistance
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string BNumber { get; set; }

        public string Email { get; set; }

        public string Image { get; set; }

        public List<Lease> Leases { get; set; }
    }
}
