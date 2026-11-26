using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Veterinaria.Datos.Models;
using Veterinaria.Datos.Repositorios;
using Veterinaria.Negocio.Mapeos;
using Veterinaria.Negocio.Seguridad;
using Veterinaria.Negocio.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // El [Required] implícito tiene su mensaje en inglés; los DTOs de entrada ya declaran los suyos
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

    // Mensajes de enlace de datos en español
    var mensajes = options.ModelBindingMessageProvider;
    mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "El cuerpo de la petición es obligatorio.");
})
.AddJsonOptions(options =>
{
    // No devuelve el detalle en inglés de System.Text.Json cuando el JSON es inválido
    options.AllowInputFormatterExceptionMessages = false;

    // Los números deben llegar como números (igual que los documenta Swagger)
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// Errores generados por ASP.NET en español. Los de las reglas de negocio se escriben en los controladores.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        var problema = context.ProblemDetails;

        problema.Title = problema.Status switch
        {
            StatusCodes.Status400BadRequest => "Los datos enviados no son válidos.",
            StatusCodes.Status401Unauthorized => "No autorizado: las credenciales o el token no son válidos.",
            StatusCodes.Status403Forbidden => "Tu rol no tiene permiso para realizar esta acción.",
            StatusCodes.Status404NotFound => "No se encontró el recurso solicitado.",
            StatusCodes.Status405MethodNotAllowed => "El método HTTP no está permitido en esta ruta.",
            StatusCodes.Status415UnsupportedMediaType => "Tipo de contenido no admitido: use application/json.",
            StatusCodes.Status500InternalServerError => "Ocurrió un error interno en el servidor.",
            _ => problema.Title
        };

        // ASP.NET usa este texto fijo cuando el JSON está mal escrito o un valor no tiene el tipo esperado
        if (problema is HttpValidationProblemDetails validacion)
        {
            foreach (var errores in validacion.Errors.Values)
            {
                for (var i = 0; i < errores.Length; i++)
                {
                    if (errores[i] == "The input was not valid.")
                    {
                        errores[i] = "El valor enviado no tiene el formato correcto.";
                    }
                }
            }
        }
    });

// Por defecto OpenAPI declara los números como ["integer", "string"] y Swagger UI no valida ese tipo
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

// OpenAPI con el esquema Bearer: Swagger UI muestra el botón Authorize para pegar el token
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((documento, contexto, cancelacion) =>
    {
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Pega el token que devuelve POST /api/auth/login (sin escribir la palabra Bearer)."
        };
        return Task.CompletedTask;
    });

    // El candado solo aparece en los endpoints que exigen token ([Authorize] sin [AllowAnonymous])
    options.AddOperationTransformer((operacion, contexto, cancelacion) =>
    {
        var metadatos = contexto.Description.ActionDescriptor.EndpointMetadata;
        if (metadatos.OfType<IAuthorizeData>().Any() && !metadatos.OfType<IAllowAnonymous>().Any())
        {
            operacion.Security ??= [];
            operacion.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", contexto.Document)] = []
            });
        }
        return Task.CompletedTask;
    });
});

// La cadena de conexión no se sube al repositorio: en local viene de user-secrets
var cadenaConexion = builder.Configuration.GetConnectionString("VeterinariaDB")
    ?? throw new InvalidOperationException("Falta configurar la cadena de conexión VeterinariaDB.");

// Sección Jwt: emisor, audiencia y minutos en appsettings.json; la clave en user-secrets (nunca en el repositorio)
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOpciones>()
    ?? throw new InvalidOperationException("Falta la sección Jwt en la configuración.");

if (jwt.Clave.Length < 32)
{
    throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres. Guárdala con dotnet user-secrets.");
}

// Autenticación JWT: valida emisor, audiencia, vencimiento y firma de cada token
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Los claims conservan los nombres del token (sub, email, role) en lugar de los URI largos de .NET
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            NameClaimType = JwtRegisteredClaimNames.Email,
            RoleClaimType = JwtOpciones.ClaimRol
        };
    });

builder.Services.AddAuthorization();

// Inyección de dependencias
// Capa de Datos: DbContext y repositorios
builder.Services.AddDbContext<VeterinariaContext>(options =>
    options.UseSqlServer(cadenaConexion));

builder.Services.AddScoped<IPropietarioRepositorio, PropietarioRepositorio>();
builder.Services.AddScoped<IMascotaRepositorio, MascotaRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

// Capa de Negocio: servicios, opciones del token y AutoMapper (registra el MappingProfile)
builder.Services.AddScoped<IPropietarioServicio, PropietarioServicio>();
builder.Services.AddScoped<IMascotaServicio, MascotaServicio>();
builder.Services.AddScoped<IAuthServicio, AuthServicio>();
builder.Services.AddSingleton(jwt);

builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.

// Errores no controlados (500) y respuestas sin cuerpo (401, 403, ruta inexistente) en JSON
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Veterinaria API v1");
        // Conserva el token pegado en Authorize aunque se recargue la página
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

// Panel web que consume la API, en la carpeta wwwroot (http://localhost:5169/)
app.UseDefaultFiles();
app.UseStaticFiles();

// Primero se identifica al usuario con su token y después se revisan sus permisos
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
