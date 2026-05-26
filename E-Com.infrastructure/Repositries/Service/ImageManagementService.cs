using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using E_Com.Core.Services;
using Microsoft.AspNetCore.Http;

namespace E_Com.infrastructure.Repositries.Service
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly Cloudinary _cloudinary;

        public ImageManagementService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<List<string>> AddImageAsync(IFormFileCollection files, string src)
        {
            var savedUrls = new List<string>();
            var folder = $"eshop/{src.Replace(" ", "_")}";

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false,
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                    throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

                savedUrls.Add(result.SecureUrl.ToString());
            }
            return savedUrls;
        }

        public void DeleteImageAsync(string src)
        {
            if (string.IsNullOrWhiteSpace(src)) return;

            try
            {
                // Extract public_id from full Cloudinary URL
                // URL: https://res.cloudinary.com/{cloud}/image/upload/v123/eshop/folder/file.jpg
                // public_id: eshop/folder/file
                var parts = src.Split("/upload/");
                if (parts.Length != 2) return;

                var pathAfterUpload = parts[1]; // "v123/eshop/folder/file.jpg"
                // Remove version segment (v followed by digits and slash)
                var slashIdx = pathAfterUpload.IndexOf('/');
                if (slashIdx >= 0 && pathAfterUpload.Substring(0, slashIdx).StartsWith("v"))
                    pathAfterUpload = pathAfterUpload.Substring(slashIdx + 1); // "eshop/folder/file.jpg"

                var lastDot = pathAfterUpload.LastIndexOf('.');
                var publicId = lastDot >= 0 ? pathAfterUpload.Substring(0, lastDot) : pathAfterUpload;

                _cloudinary.Destroy(new DeletionParams(publicId));
            }
            catch { }
        }
    }
}
