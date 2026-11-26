using Veterinaria.Entidades.Dtos;

namespace Veterinaria.Negocio.Servicios;

public interface IAuthServicio
{
    Task<UsuarioDto> RegistrarAsync(RegistroDto dto);

    // Devuelve null si el correo no existe o si la clave es incorrecta
    Task<TokenDto?> LoginAsync(LoginDto dto);
}
