using com.sun.org.apache.xerces.@internal.impl.dv.util;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicBack.Entities
{
    public class Doctor
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string Idnumber { get; set; }
        public string Email { get; set; }
        public string Categories { get; set; }
        public int Views { get; set; }
        public string Description { get; set; }
        public string Photo { get; set; }
        public string Cvfile { get; set; }
        public string Role { get; set; }
        public string Security { get; set; }

    }


}
