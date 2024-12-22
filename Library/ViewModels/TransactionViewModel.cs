namespace Library.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string UserNationalId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public Models.TransactionStatus Status { get; set; }
        public List<TransactionItemViewModel> Items { get; set; }
        public int ItemCount => Items?.Sum(item => item.Quantity) ?? 0;

        public TransactionViewModel(string userNationalId, Models.TransactionStatus status)
        {
            UserNationalId = userNationalId;
            TransactionDate = DateTime.Now;
            Status = status;
            Items = new List<TransactionItemViewModel>();
        }
    }
}