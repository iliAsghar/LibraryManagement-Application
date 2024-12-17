namespace Library.ViewModels
{
    public class UserViewModel
    {
        public string? NationalId { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public List<Models.Transaction>? Transactions { get; set; }
    }
}
