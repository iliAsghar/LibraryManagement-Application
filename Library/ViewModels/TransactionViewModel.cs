namespace Library.ViewModels
{
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string UserNationalId { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime DeliverDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public Models.TransactionStatus Status { get; set; }
        public List<TransactionItemViewModel> Items { get; set; }
        public int ItemCount => Items?.Sum(item => item.Quantity) ?? 0;

        public TransactionViewModel(string userNationalId, Models.TransactionStatus status)
        {
            UserNationalId = userNationalId;
            Status = status;
            Items = new List<TransactionItemViewModel>();
        }
    }
}