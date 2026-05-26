using E_Com.Core.Entites;
using E_Com.infrastructure;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.RateLimiting;

namespace E_Com.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CORS — reads origin from environment variable
            var allowedOrigin = Environment.GetEnvironmentVariable("CORS__AllowedOrigin")
                                ?? "https://e-com-app-ngx.onrender.com";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CORSPolicy", policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .WithOrigins(allowedOrigin);
                });
            });

            // Rate Limiting — 5 requests per minute on auth endpoints
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("auth", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.infrastructureConfiguration(builder.Configuration);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            var app = builder.Build();

            // Global exception handler — never expose stack trace to client
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;
                    Console.WriteLine($"[ERROR] {error?.Message}");
                    Console.WriteLine($"[STACK] {error?.StackTrace}");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = "An unexpected error occurred."
                    });
                });
            });

            using (var scope = app.Services.CreateScope())
            {
                // Migration — separate try so a transient DB error doesn't block seeding
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                    Console.WriteLine("✅ Database migration completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠️ Migration warning (may already be applied): " + ex.Message);
                }

                // Role + admin seed — runs independently of migration result
                try
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    foreach (var role in new[] { "Admin", "User" })
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            await roleManager.CreateAsync(new IdentityRole(role));
                            Console.WriteLine($"✅ Role '{role}' created.");
                        }
                    }

                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                    const string adminEmail = "admin@eshop.com";
                    if (await userManager.FindByEmailAsync(adminEmail) == null)
                    {
                        var adminUser = new AppUser
                        {
                            UserName = "admin",
                            Email = adminEmail,
                            EmailConfirmed = true,
                            DisplayName = "Admin",
                        };
                        var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            Console.WriteLine("✅ Admin user seeded.");
                        }
                        else
                        {
                            Console.WriteLine("❌ Admin seed failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ Admin user already exists.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Seed error: " + ex.Message);
                }
            }

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("CORSPolicy");
            app.UseStaticFiles();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.RunAsync();
        }
    }
}
