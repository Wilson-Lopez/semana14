namespace Veterinaria.Negocio.Seguridad;

// Roles del negocio. Se usan en el servicio (rol por defecto del registro)
// y en los controladores ([Authorize(Roles = ...)]), así el nombre se escribe una sola vez.
public static class Roles
{
    // Atiende en el mostrador: registra y actualiza a los propietarios y sus mascotas
    public const string Recepcionista = "Recepcionista";

    // Atiende a los pacientes: actualiza los datos de las mascotas y da de baja sus registros
    public const string Veterinario = "Veterinario";
}
