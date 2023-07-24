namespace ClinicBack.DTOS
{
    public class EditData
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Categories { get; set; }
        public string Role { get; set; }
        public IFormFile Photo { get; set; }
    }
}
