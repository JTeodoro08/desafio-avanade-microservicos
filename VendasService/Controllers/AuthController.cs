using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // 🔐 Mesma chave usada no Program.cs
    private readonly string _secret = "SUA_CHAVE_SECRETA_SUPERFORTE_32CHARS!";

    [HttpPost("login")]
    public IActionResult Login([FromBody] UsuarioLogin login)
    {
        // Login simples para teste
        if (login.Username == "admin" && login.Password == "1234")
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, login.Username)
                }),
                Expires = DateTime.UtcNow.AddHours(24),

                // 🔹 Iguais ao Program.cs
                Issuer = "DesafioAvanade",
                Audience = "ClienteAPI",

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
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

