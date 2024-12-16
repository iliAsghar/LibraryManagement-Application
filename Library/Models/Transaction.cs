using Microsoft.EntityFrameworkCore;

namespace Library.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Status { get; set; }
        public User? User { get; set; }
        public List<TransactionItem> TransactionItems { get; set; }

        public Transaction() 
        {
            TransactionItems = new List<TransactionItem>();
        }
    }
}
