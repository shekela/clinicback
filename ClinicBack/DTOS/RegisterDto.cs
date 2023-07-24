using System.ComponentModel.DataAnnotations;

namespace ClinicBack.DTOS
{
    public class RegisterDto
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Idnumber { get; set; }
        public string Otp { get; set; }
    }
}
