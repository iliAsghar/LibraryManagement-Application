namespace Library.ViewModels
{
    public class TransactionItemViewModel
    {
        public int Id { get; set; }
        public required string BookTitle { get; set; }
        public required string Description { get; set; }
        public required string Author { get; set; }
        public int Quantity { get; set; }
        public string? BookCoverPath { get; set; }
    }
}
