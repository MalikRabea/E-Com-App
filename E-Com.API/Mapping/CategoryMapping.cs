using AutoMapper;
using E_Com.Core.DTO;
using E_Com.Core.Entites.Products;

namespace E_Com.API.Mapping
{
    public class CategoryMapping:Profile
    {
        public CategoryMapping()
        {
            CreateMap<CategoryDTO, Category>().ReverseMap();
            CreateMap<UpdateCategoryDTO, Category>().ReverseMap();
                
        }
    }
}
