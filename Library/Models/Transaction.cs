﻿using Microsoft.EntityFrameworkCore;

namespace Library.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime ApproveDate { get; set; }
        public DateTime RejectDate { get; set; }
        public DateTime DeliverDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public TransactionStatus Status { get; set; }
        public User? User { get; set; }
        public List<TransactionItem> TransactionItems { get; set; }
        public int ItemCount => TransactionItems?.Sum(item => item.Quantity) ?? 0;

        public Transaction() 
        {
            Status = TransactionStatus.UnFinalized;
            TransactionItems = new List<TransactionItem>();
        }
    }
}
