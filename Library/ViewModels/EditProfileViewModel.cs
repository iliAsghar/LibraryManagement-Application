using System.ComponentModel.DataAnnotations;

namespace Library.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "ایمیل اجباری است")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
        public string Email { get; set; }

        [Required(ErrorMessage = "شماره تماس اجباری است")]
        [Phone(ErrorMessage = "فرمت شماره تلفن صحیح نیست")]
        public long PhoneNumber { get; set; }

        [Required(ErrorMessage = "نام اجباری است")]
        public string Name { get; set; }

        [Required(ErrorMessage = "نام خانوادگی اجباری است")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "کد ملی اجباری است")]
        public string NationalId { get; set; }
        public string? PfpPath { get; set; }
    }
}