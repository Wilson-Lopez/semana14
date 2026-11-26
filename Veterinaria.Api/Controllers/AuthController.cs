using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Entidades.Dtos;
using Veterinaria.Negocio.Excepciones;
using Veterinaria.Negocio.Seguridad;
using Veterinaria.Negocio.Servicios;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthServicio _authServicio;

    public AuthController(IAuthServicio authServicio)
    {
        _authServicio = authServicio;
    }

    // POST: api/auth/registro  (público)
    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Registro(RegistroDto dto)
    {
        try
        {
            var usuario = await _authServicio.RegistrarAsync(dto);
            return StatusCode(StatusCodes.Status201Created, usuario);
        }
        catch (ReglaNegocioException ex)
        {
            // La regla incumplida (correo repetido) se convierte en 409 Conflict
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Se incumplió una regla de negocio.",
                Detail = ex.Message
            });
        }
    }

    // POST: api/auth/login  (público)
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto)
    {
        var token = await _authServicio.LoginAsync(dto);

        // Misma respuesta si el correo no existe o si la clave es incorrecta: no revela cuál falló
        if (token == null)
        {
            return Unauthorized();
        }

        return Ok(token);
    }

    // GET: api/auth/perfil  (solo autenticados)
    // Lee el correo y el rol de los claims del token, sin consultar la base de datos
    [HttpGet("perfil")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<PerfilDto> Perfil()
    {
        var perfil = new PerfilDto
        {
            Correo = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty,
            Rol = User.FindFirstValue(JwtOpciones.ClaimRol) ?? string.Empty
        };

        return Ok(perfil);
    }
}
