using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UsuarioLogin login)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔹 Login simplificado apenas para testes
            if (login.Username == "admin" && login.Password == "1234")
            {
                // ✅ Corrigido: usar "Key" que está no appsettings.json
                var secret = _configuration["Jwt:Key"] 
                    ?? throw new Exception("Chave JWT não configurada no appsettings.json");
                
                var key = Encoding.UTF8.GetBytes(secret);

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, login.Username)
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    Issuer = _configuration["Jwt:Issuer"] ?? "DesafioAvanade",
                    Audience = _configuration["Jwt:Audience"] ?? "ClienteAPI",
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                    )
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new { Token = tokenString });
            }

            return Unauthorized();
        }
    }

    // Modelo simples para login
    public class UsuarioLogin
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}


