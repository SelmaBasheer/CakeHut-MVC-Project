using System.ComponentModel.DataAnnotations;

namespace CakeHut.Models
{
    public class VerifyOtpDto
    {
        [Required]
        [Display(Name = "OTP")]
        public string Otp { get; set; }
    }
}
