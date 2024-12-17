namespace Library.ViewModels
{
    public class BookViewModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public required string Author { get; set; }
        public string? CoverPath { get; set; }
        public int TotalQuantity { get; set; }
    }
}
