using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Entidades.Dtos;

// DTO de entrada para registrar un usuario (POST api/auth/registro).
// No tiene Rol: el rol lo asigna el servidor, nunca el cliente.
public class RegistroDto
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clave es obligatoria.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La clave debe tener entre 8 y 64 caracteres.")]
    public string Clave { get; set; } = string.Empty;
}
