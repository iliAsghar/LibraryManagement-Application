using Microsoft.EntityFrameworkCore;

namespace Library.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required DateTime TransactionDate { get; set; }
        public TransactionStatus Status { get; set; }
        public User? User { get; set; }
        public List<TransactionItem> TransactionItems { get; set; }

        public Transaction() 
        {
            Status = TransactionStatus.UnFinalized;
            TransactionItems = new List<TransactionItem>();
        }
    }
}
