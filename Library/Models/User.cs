using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public string PhoneNumber { get; set; }
        public required string Password { get; set; }
        public required string Name { get; set; }
        public required string Lastname { get; set; }
        public required string NationalId { get; set; }
        public required string Role { get; set; }
        public string? PfpPath { get; set; }

        public List<Transaction>? Transactions { get; set; }
    }
}
