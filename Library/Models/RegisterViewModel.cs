using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class RegisterViewModel
    {
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نمی‌باشد.")]
        [Display(Name = "ایمیل")]
        [Required(ErrorMessage = "لطفا ایمیل خود را وارد کنید!")]
        public required string Email { get; set; }

        [MaxLength(20)]
        [Display(Name = "نام")]
        [Required(ErrorMessage = "لطفا نام خود را وارد کنید!")]
        public required string Name { get; set; }

        [MaxLength(20)]
        [Display(Name = "نام خانوادگی")]
        [Required(ErrorMessage = "لطفا نام خانوادگی خود را وارد کنید!")]
        public required string LastName { get; set; }

        [MaxLength(10)]
        [Display(Name = "کد ملی")]
        [Required(ErrorMessage = "لطفا کد ملی خود را وارد کنید!")]
        public required string NationalId { get; set; }

        [MaxLength(50)]
        [DataType(DataType.Password)]
        [Display(Name = "کلمه عبور")]
        [Required(ErrorMessage = "لطفا رمز عبور خود را وارد کنید!")]
        public required string Password { get; set; }

        [MaxLength(50)]
        [DataType(DataType.Password)]
        [Display(Name = "تایید کلمه عبور")]
        [Compare("Password", ErrorMessage = "رمز عبور و تایید آن یکسان نیست!")]
        [Required(ErrorMessage = "لطفا تایید کلمه عبور را وارد کنید!")]
        public required string ConfirmPassword { get; set; }
    }
}