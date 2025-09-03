using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E_Com.Core.Entites;
using E_Com.Core.interfaces;
using E_Com.Core.Services;
using E_Com.infrastructure.Data;
using E_Com.infrastructure.Repositries;
using E_Com.infrastructure.Repositries.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace E_Com.infrastructure
{
    public static class infrastructureRegisteration
    {
        public static IServiceCollection infrastructureConfiguration(this IServiceCollection services , IConfiguration configuration  )
        {
            services.AddScoped(typeof(IGenericRepositry<>), typeof(GenericRepositry<>));

          

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // Register the email sender
            services.AddScoped<IEmailService, EmailService>();
            //Register rating for product
            services.AddScoped<IRating, RatingRepositry>();


            //register Iorder service
            services.AddScoped<IOrderService, OrderService>();

            // Register the token generator
            services.AddScoped<IGenerateToken, GenerateToken>();

            // Register payment service
            services.AddScoped<IPaymentService, PaymentService>();

            services.AddScoped<IProductRepositry, ProductRepositry>();
            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<IFavoriteRepository, FavoriteRepository>();

            //apply Redis Connection
            services.AddSingleton<IConnectionMultiplexer>(i =>
            {
                var config = ConfigurationOptions.Parse(configuration.GetConnectionString("redis"));
                return ConnectionMultiplexer.Connect(config);
            });



            services.AddSingleton<IImageManagementService, ImageManagementService>();
            services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                );

            var connectionString = configuration.GetConnectionString("EcomDatabase");

            // DbContext مع MySQL
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, MySqlServerVersion.AutoDetect(connectionString))
            );
    
            // هنا تحقن أي services أخرى في Infrastructure
            // services.AddScoped<IYourService, YourService>();

        

        services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();


            services.AddAuthentication(op =>
            {
                op.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddCookie(op => {

                op.Cookie.Name = "token";
                op.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized; // Unauthorized
                    return Task.CompletedTask;
                };

            }).AddJwtBearer(op =>
            {
                op.RequireHttpsMetadata = false;
                op.SaveToken = true;
                op.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Token:Secret"])),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Token:Issuer"],
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // Optional: Set clock skew to zero for immediate expiration
                };
                op.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["token"];
                        return Task.CompletedTask;
                    }
                };
            });



            return services;
        }
    }
}
