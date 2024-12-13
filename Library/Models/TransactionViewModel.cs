namespace Library.Models
{
    public class TransactionViewModel
    {
        public Transaction? Transaction { get; set; }
        public List<TransactionItem>? TransactionItems { get; set; }
        public List<Book>? Books { get; set; }
    }
}
