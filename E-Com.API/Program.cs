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

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("CORSPolicy");

            app.UseHttpsRedirection();   // moved up
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
