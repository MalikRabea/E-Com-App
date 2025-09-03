using AutoMapper;
using E_Com.API.Helper;
using E_Com.Core.DTO;
using E_Com.Core.Entites.Products;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.Core.Sharing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Com.API.Controllers
{

    public class ProductsController : BaseController
    {
        private readonly IImageManagementService service;
        private readonly IProductService _productService;

        public ProductsController(IUnitOfWork work, IMapper mapper, IImageManagementService service, IProductService productService) : base(work, mapper)
        {
            this.service = service;
            _productService = productService;
        }
        [HttpGet("get-all")]
        public async Task<IActionResult> get([FromQuery]ProductParams productParams)
        {
            try
            {
                var Product = await work.ProductRepositry
                    .GetAllAsync(productParams);
               
                return Ok(new Pagination<ProductDTO>(productParams.PageNumber,productParams.pageSize,Product.TotalCount, Product.products));

            }
            catch (Exception ex)
            {
                return BadRequest (ex.Message);
            }
        }
        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> getById(int id)
        {
            try
            {
                var Product = await work.ProductRepositry
                    .GetByIdAsync(id, x => x.Category, x => x.Photos);
                var result = mapper.Map<ProductDTO>(Product);

                if (Product is null)
                {
                    return BadRequest(new ResponseAPI(400));
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseAPI(400, ex.Message));
            }
        }
        [HttpPost("add-Product")]
        public async Task<IActionResult> add(AddProductDTO productDTO)
        {
            try
            {
                await work.ProductRepositry.AddAsync(productDTO);
                return Ok(new ResponseAPI(200 ));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseAPI(400, ex.Message));
            }
        }

        [HttpPut("update-Product")]
        public async Task<IActionResult> update(UpdateProductDTO updateProductDTO)
        {
            try
            {
                await work.ProductRepositry.UpdateAsync(updateProductDTO);
                return Ok(new ResponseAPI(200));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseAPI(400, ex.Message));
            }
        }
        [HttpDelete("delete-Product/{Id}")]
        public async Task<IActionResult> delete(int Id)
        {
            try
            {
               var product = await work.ProductRepositry.GetByIdAsync(Id , x=>x.Photos , x=>x.Category);
                await work.ProductRepositry.DeleteAsync(product);
                return Ok(new ResponseAPI(200));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseAPI(400, ex.Message));
            }
        }

        [HttpGet("best-sellers")]
        public async Task<IActionResult> GetBestSellers()
        {
            var products = await _productService.GetBestSellersAsync();
            return Ok(products);
        }

    }

}
