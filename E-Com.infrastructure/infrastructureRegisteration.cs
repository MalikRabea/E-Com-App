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

public static class infrastructureRegisteration
{
    public static IServiceCollection infrastructureConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IGenericRepositry<>), typeof(GenericRepositry<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRating, RatingRepositry>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IGenerateToken, GenerateToken>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IProductRepositry, ProductRepositry>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();

        // Redis
        var redisConfig = Environment.GetEnvironmentVariable("ConnectionStrings__redis");
        services.AddSingleton<IConnectionMultiplexer>(i =>
        {
            var config = StackExchange.Redis.ConfigurationOptions.Parse(redisConfig);
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<IImageManagementService, ImageManagementService>();
        services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

        // MySQL
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__EcomDatabase");
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, MySqlServerVersion.AutoDetect(connectionString))
        );

        services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT Auth
        var tokenSecret = Environment.GetEnvironmentVariable("Token__Secret");
        var tokenIssuer = Environment.GetEnvironmentVariable("Token__Issuer");

        services.AddAuthentication(op =>
        {
            op.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            op.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie(op =>
        {
            op.Cookie.Name = "token";
            op.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        })
        .AddJwtBearer(op =>
        {
            op.RequireHttpsMetadata = false;
            op.SaveToken = true;
            op.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(tokenSecret)),
                ValidateIssuer = true,
                ValidIssuer = tokenIssuer,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
            op.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
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
