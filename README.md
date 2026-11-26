# Veterinaria API con JWT (Semana 14)

Web API REST en capas con ASP.NET Core (.NET 10), Entity Framework Core 10 (base de datos primero), DTOs y AutoMapper para la gestión de **propietarios** y sus **mascotas** en una clínica veterinaria. En la Semana 14 se protege con **autenticación JWT**: registro e inicio de sesión de usuarios, token firmado y permisos por rol.

## Estructura de la solución

| Proyecto | Contenido | Referencia a |
|---|---|---|
| `Veterinaria.Entidades` | Entidades (`Propietario`, `Mascota`, `Usuario`) y DTOs (`Dtos/`) | — |
| `Veterinaria.Datos` | `VeterinariaContext` y repositorios con sus interfaces (incluye `UsuarioRepositorio`) | Entidades |
| `Veterinaria.Negocio` | Servicios con sus interfaces (incluye `AuthServicio`), reglas de negocio, excepciones, `MappingProfile` y seguridad (`Roles`, `JwtOpciones`) | Datos |
| `Veterinaria.Api` | Controladores (incluye `AuthController`), `Program.cs`, pruebas `Veterinaria.Api.http` y panel web (`wwwroot`) | Negocio |

Relación: un propietario (1) tiene muchas mascotas (N), mediante la clave foránea `Mascotas.PropietarioId`. La tabla `Usuarios` guarda el correo (único), el hash BCrypt de la clave y el rol.

## Roles

| Rol | Qué hace en la clínica |
|---|---|
| **Recepcionista** | Atiende el mostrador: registra, actualiza y da de baja a los propietarios, y registra a sus mascotas. |
| **Veterinario** | Atiende a los pacientes: actualiza los datos de las mascotas (peso, raza, etc.) y da de baja sus registros. |

El rol lo asigna el servidor: todo registro nuevo es Recepcionista. Para que un usuario sea Veterinario, el administrador de la base le cambia el rol con un `UPDATE`.

## Matriz de permisos

| Endpoint | Sin token | Rol Recepcionista | Rol Veterinario |
|---|---|---|---|
| POST /api/auth/registro | Sí | Sí | Sí |
| POST /api/auth/login | Sí | Sí | Sí |
| GET /api/auth/perfil | No (401) | Sí | Sí |
| GET /api/propietarios | No (401) | Sí | Sí |
| GET /api/propietarios/{id} | No (401) | Sí | Sí |
| GET /api/propietarios/{id}/mascotas | No (401) | Sí | Sí |
| POST /api/propietarios | No (401) | Sí | No (403) |
| PUT /api/propietarios/{id} | No (401) | Sí | No (403) |
| DELETE /api/propietarios/{id} | No (401) | Sí | No (403) |
| GET /api/mascotas | No (401) | Sí | Sí |
| GET /api/mascotas/{id} | No (401) | Sí | Sí |
| POST /api/mascotas | No (401) | Sí | Sí |
| PUT /api/mascotas/{id} | No (401) | No (403) | Sí |
| DELETE /api/mascotas/{id} | No (401) | No (403) | Sí |

## Reglas de negocio (409 Conflict)

1. No se puede registrar un propietario con un DNI que ya existe.
2. No se puede eliminar un propietario que tiene mascotas registradas.
3. Un propietario no puede tener dos mascotas con el mismo nombre (al crear o actualizar).
4. No se puede registrar un usuario con un correo que ya existe.

## Cómo ejecutarla

1. Crear la base de datos con sus datos de ejemplo (incluye un usuario por rol):

   ```
   sqlcmd -S .\SQLEXPRESS -E -C -f 65001 -i VeterinariaDB.sql
   ```

2. Guardar la cadena de conexión y la clave del token en user-secrets (no se suben al repositorio). Reemplaza los valores entre `< >`; la clave debe tener al menos 32 caracteres:

   ```
   dotnet user-secrets set "ConnectionStrings:VeterinariaDB" "<cadena de conexión>" --project Veterinaria.Api
   dotnet user-secrets set "Jwt:Clave" "<clave secreta de 32 caracteres o más>" --project Veterinaria.Api
   ```

   El emisor, la audiencia y los minutos de vida del token están en la sección `Jwt` de `appsettings.json`.

3. Ejecutar la API:

   ```
   dotnet run --project Veterinaria.Api --launch-profile http
   ```

   - Panel web (pide iniciar sesión): `http://localhost:5169/`
   - Swagger (botón **Authorize** para pegar el token): `http://localhost:5169/swagger`

4. Probar los endpoints en orden con `Veterinaria.Api/Veterinaria.Api.http`: primero inicia sesión con cada rol y las demás peticiones usan esos tokens. Vuelve a ejecutar el script SQL antes de repetir las pruebas.

## Panel web

`Veterinaria.Api/wwwroot` contiene un panel (HTML, CSS y JavaScript sin librerías) que consume la API con `fetch` desde el mismo sitio, por eso no necesita CORS. Pide iniciar sesión, envía el token en cada petición, muestra el correo y el rol del usuario (de `GET api/auth/perfil`) y solo muestra los botones que permite su rol; la API vuelve a validarlo con `[Authorize]`. Si el token vence o no es válido (401), vuelve a la pantalla de inicio de sesión.

## Endpoints

| Método | Ruta | Códigos |
|---|---|---|
| POST | `api/auth/registro` | 201, 400, 409 |
| POST | `api/auth/login` | 200, 400, 401 |
| GET | `api/auth/perfil` | 200, 401 |
| GET | `api/propietarios?nombres=&dni=` | 200, 401 |
| GET | `api/propietarios/{id}` | 200, 401, 404 |
| GET | `api/propietarios/{id}/mascotas` | 200, 401, 404 |
| POST | `api/propietarios` | 201, 400, 401, 403, 409 |
| PUT | `api/propietarios/{id}` | 204, 400, 401, 403, 404 |
| DELETE | `api/propietarios/{id}` | 204, 401, 403, 404, 409 |
| GET | `api/mascotas?especie=&raza=&nombre=` | 200, 401 |
| GET | `api/mascotas/{id}` | 200, 401, 404 |
| POST | `api/mascotas` | 201, 400, 401, 409 |
| PUT | `api/mascotas/{id}` | 204, 400, 401, 403, 404, 409 |
| DELETE | `api/mascotas/{id}` | 204, 401, 403, 404 |
