using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Library.Models
{
    public class TransactionItem
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public int BookId { get; set; }
        public int Quantity { get; set; }

        public Book? Book { get; set; }
        public Transaction? Transaction { get; set; }
    }
}
