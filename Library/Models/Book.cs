﻿using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class Book
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Author { get; set; }
        public string Genre { get; set; }
        public string? CoverPath { get; set; }
        public required int TotalQuantity { get; set; }
        public List<TransactionItem>? TransactionItems { get; set; } = new List<TransactionItem>();
    }
}