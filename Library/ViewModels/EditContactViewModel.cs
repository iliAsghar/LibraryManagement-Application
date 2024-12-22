using System.ComponentModel.DataAnnotations;

public class EditContactViewModel
{
    [Required(ErrorMessage = "عنوان اجباری است")]
    public string Title { get; set; }

    [Required(ErrorMessage = "توضیحات اجباری است")]
    public string Description { get; set; }

    [Required(ErrorMessage = "آدرس اجباری است")]
    public string Address { get; set; }

    [Required(ErrorMessage = "شماره تلفن اجباری است")]
    [Phone(ErrorMessage = "فرمت شماره تلفن صحیح نیست")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "ایمیل اجباری است")]
    [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست")]
    public string Email { get; set; }
}