using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Datos.Repositorios;
using Veterinaria.Entidades;
using Veterinaria.Entidades.Dtos;
using Veterinaria.Negocio.Excepciones;
using Veterinaria.Negocio.Seguridad;

namespace Veterinaria.Negocio.Servicios;

public class AuthServicio : IAuthServicio
{
    // Hash de una clave cualquiera. Se verifica cuando el correo no existe,
    // así el login tarda lo mismo y no revela qué correos están registrados.
    private static readonly string HashFicticio = BCrypt.Net.BCrypt.HashPassword("clave-ficticia-para-igualar-tiempos");

    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IMapper _mapper;
    private readonly JwtOpciones _jwt;

    public AuthServicio(IUsuarioRepositorio usuarioRepositorio, IMapper mapper, JwtOpciones jwt)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _mapper = mapper;
        _jwt = jwt;
    }

    public async Task<UsuarioDto> RegistrarAsync(RegistroDto dto)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();

        // Regla de negocio: el correo no se puede repetir
        if (await _usuarioRepositorio.BuscarPorCorreoAsync(correo) != null)
        {
            throw new ReglaNegocioException($"Ya existe un usuario registrado con el correo {correo}.");
        }

        var usuario = _mapper.Map<Usuario>(dto);
        usuario.Correo = correo;

        // La clave se guarda como hash BCrypt (lleva su propia sal), nunca en texto
        usuario.ClaveHash = BCrypt.Net.BCrypt.HashPassword(dto.Clave);

        // El rol lo decide el servidor: todo usuario nuevo es Recepcionista.
        // Para que sea Veterinario, el administrador de la base le cambia el rol con un UPDATE.
        usuario.Rol = Roles.Recepcionista;

        await _usuarioRepositorio.AgregarAsync(usuario);
        await _usuarioRepositorio.GuardarAsync();

        return _mapper.Map<UsuarioDto>(usuario);
    }

    public async Task<TokenDto?> LoginAsync(LoginDto dto)
    {
        var usuario = await _usuarioRepositorio.BuscarPorCorreoAsync(dto.Correo.Trim().ToLowerInvariant());

        // Si el correo no existe se verifica contra el hash ficticio: el resultado es el mismo (null)
        var claveValida = BCrypt.Net.BCrypt.Verify(dto.Clave, usuario?.ClaveHash ?? HashFicticio);

        if (usuario == null || !claveValida)
        {
            return null;
        }

        var expiracion = DateTime.UtcNow.AddMinutes(_jwt.MinutosExpiracion);

        // Claims: quién es (sub), su correo (email) y su rol (role). Nunca la clave ni datos sensibles.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(JwtOpciones.ClaimRol, usuario.Rol),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Firma HMAC-SHA256 con la clave secreta de la configuración
        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Clave));
        var token = new JwtSecurityToken(
            issuer: _jwt.Emisor,
            audience: _jwt.Audiencia,
            claims: claims,
            expires: expiracion,
            signingCredentials: new SigningCredentials(llave, SecurityAlgorithms.HmacSha256));

        return new TokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiracion = expiracion
        };
    }
}
