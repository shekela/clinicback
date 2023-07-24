using CloudinaryDotNet.Actions;

namespace ClinicBack.Interfaces
{
    public interface IPhotoService
    {
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);
    }
}
