using ClinicBack.Entities;

namespace ClinicBack.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(Client token);
        string CreateTokenDoctor(Doctor token);
        string CreateTokenAdmin(Admin token);
    }
}
