using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Application.Services;
using PasswordManagerSystem.Api.Infrastructure.Authentication;
using PasswordManagerSystem.Api.Infrastructure.Data;
using PasswordManagerSystem.Api.Infrastructure.Security;

namespace PasswordManagerSystem.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

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

            builder.Services.AddScoped<IRoleResolverService, RoleResolverService>();
            builder.Services.AddScoped<IUserSyncService, UserSyncService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

            var jwtSecret = builder.Configuration["Jwt:Secret"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                throw new InvalidOperationException("JWT secret is not configured.");
            }

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSecret)
                        ),

                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}