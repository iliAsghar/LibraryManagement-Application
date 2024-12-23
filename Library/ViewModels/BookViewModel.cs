using Library.Models;

namespace Library.ViewModels
{
    public class BookViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public required string Author { get; set; }
        public string Genre { get; set; }
        public string? CoverPath { get; set; }
        public int TotalQuantity { get; set; }
    }
}
