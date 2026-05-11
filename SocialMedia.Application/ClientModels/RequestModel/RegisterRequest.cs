using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SocialMedia.Application.ClientModels.RequestModel
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*\d)(?=.*[a-zA-Z]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain at least one letter and one digit")]
        [DataType(DataType.Password)]
        [DefaultValue("Password123")]
        public string Password { get; set; }
    }
}
