using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace E_Com.infrastructure.Repositries.Service
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IFileProvider fileProvider;
        public ImageManagementService(IFileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
        }
        public async Task<List<string>> AddImageAsync(IFormFileCollection files, string src)
        {
           var SaveImage = new List<string>();
            var ImageDirectory = Path.Combine("wwwroot" ,"Images" ,src);
            if (Directory.Exists(ImageDirectory)is not true)
            {
                Directory.CreateDirectory(ImageDirectory);
            }
            foreach (var item in files)
            {
                if (item.Length > 0)
                {   
                   var Imagename = item.FileName;  
                    var ImageSrc = $"/Images/{src}/{Imagename}";
                    var root = Path.Combine(ImageDirectory, Imagename);
                    using (FileStream stream = new FileStream(root, FileMode.Create))
                    {
                        await item.CopyToAsync(stream);
                    }
                    SaveImage.Add(ImageSrc);
                }
            }
            return SaveImage;
        }

        public void DeleteImageAsync(string src)
        {
            var info = fileProvider.GetFileInfo(src);
            var root = info.PhysicalPath;
           
                File.Delete(root);
           
        }
    }   
} 
