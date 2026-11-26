namespace Veterinaria.Entidades.Dtos;

// Respuesta del login: el token JWT y cuándo vence (en UTC)
public class TokenDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expiracion { get; set; }
}
