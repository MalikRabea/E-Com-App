using E_Com.API.Middleware;
using E_Com.infrastructure;
using E_Com.infrastructure.Data;
using Microsoft.EntityFrameworkCore; // ??? ???? ?? Migrate()


using Microsoft.Extensions.FileProviders;
namespace E_Com.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
           
            // Add services to the container.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CORSPolicy",
                    builder =>
                    {
                               builder.AllowAnyHeader()
                                      .AllowAnyMethod()
                                      .AllowCredentials()
                                      .WithOrigins("http://localhost:4200");
                    });
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.infrastructureConfiguration(builder.Configuration);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate(); // ???? ??????? ???????? ??? ????? MySQL
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("CORSPolicy");
            app.UseMiddleware<ExceptionsMiddleware>();
            app.UseAuthentication();

            app.UseAuthorization();


            app.UseStaticFiles();
            app.UseStatusCodePagesWithReExecute("/errors/{0}");
            app.UseHttpsRedirection();

           


            app.MapControllers();

            app.Run();
        }
    }
}
