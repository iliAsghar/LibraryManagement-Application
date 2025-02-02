﻿using Library.Models;
using System.ComponentModel.DataAnnotations;

namespace Library.ViewModels
{
    public class AddBookViewModel
    {
        [MaxLength(30)]
        [Display(Name = "نام کتاب")]
        [Required(ErrorMessage = "عنوان کتاب الزامی است!")]
        public string? Title { get; set; }

        [MaxLength(500)]
        [Display(Name = "توضیحات")]
        [Required(ErrorMessage = "توضیحات کتاب الزامی است!")]
        public string? Description { get; set; }

        [MaxLength(30)]
        [Display(Name = "نام نویسنده")]
        [Required(ErrorMessage = "نام نویسنده الزامی است!")]
        public string? Author { get; set; }

        [Display(Name = "تصویر جلد")]
        public string? CoverPath { get; set; }

        [Display(Name = "ژانر کتاب")]
        public string Genre { get; set; }


        [Display(Name = "تعداد موجودی")]
        [Range(0, int.MaxValue, ErrorMessage = "تعداد موجودی نمیتواند منفی باشد!")]
        [Required(ErrorMessage = "تعداد موجودی الزامی است!")]
        public int TotalQuantity { get; set; }
    }
}
