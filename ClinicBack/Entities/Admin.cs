namespace ClinicBack.Entities
{
    public class Admin
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string Role { get; set; }
        public string Photo { get; set; }
        public string Security { get; set; }

    }
}
