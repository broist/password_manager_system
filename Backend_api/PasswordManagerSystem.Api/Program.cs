using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Infrastructure.Data;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Infrastructure.Authentication;


namespace PasswordManagerSystem.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString)
                );
            });

            var authenticationProvider = builder.Configuration["Authentication:Provider"];

            if (authenticationProvider == "Mock")
            {
                builder.Services.AddScoped<IAdAuthenticationService, MockAdAuthenticationService>();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported authentication provider: {authenticationProvider}"
                );
            }
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
