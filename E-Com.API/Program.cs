using E_Com.API.Middleware;
using E_Com.infrastructure;
using E_Com.infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace E_Com.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CORSPolicy", policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .WithOrigins("https://e-com-app-ngx.onrender.com");
                });
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.infrastructureConfiguration(builder.Configuration);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            var app = builder.Build();


            // Global exception handler (??? ?? middleware)
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;

                    Console.WriteLine($"[ERROR] {error?.Message}\n{error?.StackTrace}");

                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = error?.Message,
                        Details = error?.StackTrace
                    });
                });
            });

            // Auto-Migrate with error handling
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                    Console.WriteLine("? Database migration completed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("? Database migration failed.");
                    Console.WriteLine("Type: " + ex.GetType().FullName);
                    Console.WriteLine("Message: " + ex.Message);
                }
            }
            Console.WriteLine("====== ENV CHECK ======");
            Console.WriteLine("Postgres: " + Environment.GetEnvironmentVariable("ConnectionStrings__EcomDatabase"));
            Console.WriteLine("Redis: " + Environment.GetEnvironmentVariable("ConnectionStrings__redis"));
            Console.WriteLine("Stripe Publish: " + Environment.GetEnvironmentVariable("StripSetting__publishKey"));
            Console.WriteLine("Stripe Secret: " + Environment.GetEnvironmentVariable("StripSetting__secretKey"));
            Console.WriteLine("Token Issuer: " + Environment.GetEnvironmentVariable("Token__Issuer"));
            Console.WriteLine("Token Secret: " + Environment.GetEnvironmentVariable("Token__Secret"));
            Console.WriteLine("Email From: " + Environment.GetEnvironmentVariable("EmailSetting__From"));
            Console.WriteLine("=======================");

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    Console.WriteLine("?? Checking DB connection...");
                    bool canConnect = db.Database.CanConnect();
                    Console.WriteLine("? DB CanConnect: " + canConnect);

                    var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                    Console.WriteLine("?? Pending migrations: " + string.Join(",", pendingMigrations));

                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine("?? There are pending migrations, applying now...");
                        db.Database.Migrate();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("? DB Exception: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

            app.Use(async (context, next) =>
            {
                Console.WriteLine($"?? Incoming Request: {context.Request.Method} {context.Request.Path}");
                await next.Invoke();
                Console.WriteLine($"?? Response: {context.Response.StatusCode}");
            });


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("CORSPolicy");

            //app.UseHttpsRedirection();   // moved up
            app.UseStaticFiles();

            app.UseMiddleware<ExceptionsMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            app.MapControllers();
            app.Run();
        }
    }
}
