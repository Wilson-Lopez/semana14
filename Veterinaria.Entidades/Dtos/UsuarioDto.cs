namespace Veterinaria.Entidades.Dtos;

// DTO de lectura del usuario: lo que devuelve el registro. Nunca incluye el hash de la clave.
public class UsuarioDto
{
    public int Id { get; set; }

    public string Correo { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}
