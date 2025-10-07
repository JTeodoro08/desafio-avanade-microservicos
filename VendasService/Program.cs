using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendasService.Data;
using VendasService.Models;
using VendasService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace VendasService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================
            // 📋 Logger limpo estilo EstoqueService
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; // timestamp
                options.IncludeScopes = false; // remove SpanId/TraceId/ParentId/ConnectionId
            });

            // =====================
            // 🔹 Filtrar logs verbosos do framework
            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
            builder.Logging.AddFilter("System", LogLevel.Warning);
            builder.Logging.AddFilter("VendasService", LogLevel.Information);

            // =====================
            // 🗄️ Configuração do DbContext
            builder.Services.AddDbContext<VendasContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // =====================
            // 🔌 Injeção de dependências
            builder.Services.AddSingleton<IRabbitMqProducerService, RabbitMqProducerService>();

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddSingleton<IEstoqueClientService, FakeEstoqueClientService>();
            }
            else
            {
                builder.Services.AddHttpClient<IEstoqueClientService, EstoqueClientService>(client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["OcelotGateway:BaseUrl"] ?? "http://localhost:5271/estoque/");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            }

            // =====================
            // 🔐 JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var keyString = jwtSettings["Key"]
                ?? throw new InvalidOperationException("⚠️ JWT Key não configurada no appsettings.json!");
            var key = Encoding.UTF8.GetBytes(keyString);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            builder.Services.AddAuthorization();

            // =====================
            // 🌐 Controllers e JSON
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Insira o token JWT desta forma: Bearer {token}"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // =====================
            // 🌍 CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // =====================
            // 🚀 Build e configuração do app
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors();
            app.MapControllers();
            app.Run();
        }
    }

    // =====================
    // 🧪 Serviço fake para desenvolvimento local
    public class FakeEstoqueClientService : IEstoqueClientService
    {
        public Task<Produto?> GetProdutoAsync(int produtoId)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [FAKE ESTOQUE] Consultando produto {produtoId}");
            return Task.FromResult<Produto?>(new Produto
            {
                Id = produtoId,
                Nome = $"Produto {produtoId}",
                Preco = 50,
                Quantidade = 100
            });
        }
    }
}
















