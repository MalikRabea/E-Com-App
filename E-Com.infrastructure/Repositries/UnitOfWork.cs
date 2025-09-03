using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;

namespace E_Com.infrastructure.Repositries
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IImageManagementService _imageManagementService;
        private readonly IConnectionMultiplexer redis;
        private readonly UserManager<AppUser> userManager;
        private readonly IEmailService emailService;
        private readonly SignInManager<AppUser> signInManager;
        private readonly IGenerateToken generateToken;




        public ICategoryRepositry CategoryRepositry{ get; }

        public IPhotoRepositry PhotoRepositry { get; }

        public IProductRepositry ProductRepositry { get; }

        public ICustomerBasketRepository CustomerBasket { get; }

        public IAuth Auth { get; }

        public UnitOfWork(AppDbContext context, IMapper mapper, IImageManagementService imageManagementService, IConnectionMultiplexer redis, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IEmailService emailService, IGenerateToken generateToken)
        {
            _context = context;
            _mapper = mapper;
            _imageManagementService = imageManagementService;
            this.redis = redis;
            this.userManager = userManager;
            this.emailService = emailService;
            this.signInManager = signInManager;
            this.generateToken = generateToken;




            CategoryRepositry = new CategoryRepositry(_context);
            PhotoRepositry = new PhotoRepositry(_context);
            ProductRepositry = new ProductRepositry(_context, _mapper, _imageManagementService);
            CustomerBasket = new CustomerBasketRepository(redis);
            Auth = new AuthRepositry(userManager, emailService, signInManager, generateToken,context);
        }
    }
}
