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
using System.Text;

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
        if (string.IsNullOrWhiteSpace(redisConfig))
        {
            Console.WriteLine("⚠️ Redis connection string is missing. Check your environment variables.");
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(i =>
            {
                var config = ConfigurationOptions.Parse(redisConfig);
                return ConnectionMultiplexer.Connect(config);
            });
        }

        services.AddSingleton<IImageManagementService, ImageManagementService>();
        services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));

        var connectionString = configuration.GetConnectionString("EcomDatabase")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__EcomDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("⚠️ PostgreSQL connection string is missing. Check your environment variables.");
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
            );
        }

        // Stripe
        var stripePublishKey = Environment.GetEnvironmentVariable("StripSetting__publishKey");
            var stripeSecretKey = Environment.GetEnvironmentVariable("StripSetting__secretKey");

        if (string.IsNullOrWhiteSpace(stripePublishKey) || string.IsNullOrWhiteSpace(stripeSecretKey))
            {
                Console.WriteLine("⚠️ Stripe configuration is missing. Check your environment variables.");
            }
            else
            {
                // خزن المفاتيح كـ Singleton عشان تقدر تستخدمهم بأي Service
                services.AddSingleton(new Dictionary<string, string>
                     {
            { "StripePublishKey", stripePublishKey },
            { "StripeSecretKey", stripeSecretKey }
                      });

                // إعداد Stripe SDK
                Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
              }

        // Identity

        services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT Auth
        var tokenSecret = Environment.GetEnvironmentVariable("Token__Secret");
        var tokenIssuer = Environment.GetEnvironmentVariable("Token__Issuer");

        if (string.IsNullOrWhiteSpace(tokenSecret) || string.IsNullOrWhiteSpace(tokenIssuer))
        {
            Console.WriteLine("⚠️ JWT configuration is missing. Check your environment variables.");
        }
        else
        {
            services.AddAuthentication(op =>
            {
                op.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                op.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = tokenIssuer,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
                op.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["token"];
                        return Task.CompletedTask;
                    }
                };
            });
        }

        return services;
    }
}
