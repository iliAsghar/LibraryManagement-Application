namespace Library.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public long PhoneNumber { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? NationalId { get; set; }
        public string? Role { get; set; }
        public string? PfpPath { get; set; }

        public List<Models.Transaction>? Transactions { get; set; }
    }
}
