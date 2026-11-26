namespace Veterinaria.Negocio.Seguridad;

// Sección "Jwt" de la configuración. Program.cs la lee y la usa para crear y para validar los tokens.
// La Clave no va en el repositorio: en local se guarda con user-secrets.
public class JwtOpciones
{
    // Nombre del claim del rol dentro del token
    public const string ClaimRol = "role";

    public string Clave { get; set; } = string.Empty;

    public string Emisor { get; set; } = string.Empty;

    public string Audiencia { get; set; } = string.Empty;

    public int MinutosExpiracion { get; set; }
}
