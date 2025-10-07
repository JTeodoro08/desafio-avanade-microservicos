using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using EstoqueService.Data;
using EstoqueService.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================
// 🧾 CONFIGURAÇÃO DE LOGS
// =====================
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
    options.SingleLine = true;
    options.IncludeScopes = false;
});

// 🔹 Filtros de Log
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None); // ❌ oculta comandos SQL
builder.Logging.AddFilter("Microsoft", LogLevel.Warning); // ⚠️ mantém avisos importantes do ASP.NET
builder.Logging.AddFilter("EstoqueService", LogLevel.Information); // ✅ mantém logs narrativos do serviço

// =====================
// 🌐 SERVIÇOS
// =====================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });

// =====================
// 💾 DATABASE CONTEXT
// =====================
builder.Services.AddDbContext<EstoqueContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(false)); // impede logs sensíveis do EF Core

// =====================
// 🐇 RABBITMQ
// =====================
// 🔹 Consumer executa em background
builder.Services.AddHostedService<RabbitMqConsumerService>();

// 🔹 Producer compartilhado em toda a aplicação
builder.Services.AddSingleton<IRabbitMqProducerService, RabbitMqProducerService>();

// =====================
// 🔐 AUTENTICAÇÃO JWT
// =====================
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
// 📘 SWAGGER + JWT
// =====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EstoqueService API",
        Version = "v1",
        Description = "API de Gestão de Estoque integrada com RabbitMQ"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT com o prefixo **'Bearer '** antes do token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// =====================
// 🌍 CORS
// =====================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// =====================
// 🚀 PIPELINE DE EXECUÇÃO
// =====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// =====================
// 🔹 Necessário para testes de integração
// =====================
public partial class Program { }












