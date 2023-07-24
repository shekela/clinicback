using com.sun.org.apache.xerces.@internal.impl.dv.util;

namespace ClinicBack.DTOS
{
    public class DoctorRegisterDto
    {
        public string Firstname { get; set; } = "";
        public string Lastname { get; set; } = "";
        public string Password { get; set; } = "";
        public string Idnumber { get; set; } = "";
        public string Email { get; set; } = "";
        public int Views { get; set; } = 0;
        public string Categories { get; set; } = "";
        public string Description { get; set; } = "";
        public IFormFile Photo { get; set; } 
        public IFormFile Cvfile { get; set; }
    }
    
}
