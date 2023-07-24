using System.ComponentModel.DataAnnotations;

namespace ClinicBack.Entities
{
    public class Client
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string Idnumber { get; set; }
        public string Photo { get; set; }
        public string Categories { get; set; }
        public string Role { get; set; }
        public string Security { get; set; }
    }
}
