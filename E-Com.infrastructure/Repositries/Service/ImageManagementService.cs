using E_Com.Core.Services;
using Microsoft.AspNetCore.Http;

namespace E_Com.infrastructure.Repositries.Service
{
    public class ImageManagementService : IImageManagementService
    {
        public async Task<List<string>> AddImageAsync(IFormFileCollection files, string src)
        {
            var result = new List<string>();
            if (files == null || files.Count == 0) return result;

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                var mimeType = file.ContentType ?? "image/jpeg";
                result.Add($"data:{mimeType};base64,{base64}");
            }
            return result;
        }

        public void DeleteImageAsync(string src)
        {
            // stored in DB — EF Core handles deletion
        }
    }
}
