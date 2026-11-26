namespace Veterinaria.Entidades.Dtos;

// Respuesta de GET api/auth/perfil: datos tomados de los claims del token, no de la base de datos
public class PerfilDto
{
    public string Correo { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}
