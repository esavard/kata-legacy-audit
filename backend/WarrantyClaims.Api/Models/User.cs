using System;

namespace WarrantyClaims.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }

        // SHA-1, unsalted. Chosen in 2019 because "MD5 is the insecure one, SHA-1 is
        // fine" - a belief that was already outdated in 2019. See AuthController for
        // where this gets hashed and compared.
        public string PasswordHash { get; set; }

        public string Role { get; set; }            // 'dealer', 'manufacturer', 'admin' - free text, not an enum
        public string FullName { get; set; }
        public int? DealerId { get; set; }           // null for manufacturer/admin accounts
        public Dealer Dealer { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
