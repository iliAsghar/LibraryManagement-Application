namespace Library.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public required string UserNationalId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public required Models.TransactionStatus Status { get; set; }
        public List<TransactionItemViewModel> Items { get; set; }

        public TransactionViewModel(string userNationalId, Models.TransactionStatus status)
        {
            UserNationalId = userNationalId;
            Status = status;
            Items = new List<TransactionItemViewModel>();
        }
    }
}
