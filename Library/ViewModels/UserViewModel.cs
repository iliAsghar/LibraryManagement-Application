namespace Library.ViewModels
{
    public class UserViewModel
    {
        public int NationalId { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public List<Models.Transaction>? Transactions { get; set; }
    }
}
