namespace Library.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public int UserNationalId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public required string Status { get; set; }
        public List<TransactionItemViewModel>? Items { get; set; }

        public TransactionViewModel()
        {
            Items = new List<TransactionItemViewModel>();
        }
    }
}
