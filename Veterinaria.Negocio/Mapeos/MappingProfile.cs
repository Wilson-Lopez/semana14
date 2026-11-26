using AutoMapper;
using Veterinaria.Entidades;
using Veterinaria.Entidades.Dtos;

namespace Veterinaria.Negocio.Mapeos;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

        // Entidad -> DTO de lectura (lo que devuelve la API)
        CreateMap<Propietario, PropietarioDto>()
            .ForMember(destino => destino.CantidadMascotas,
                       opciones => opciones.MapFrom(origen => origen.Mascotas.Count));

        // DTO de entrada -> Entidad (POST)
        CreateMap<PropietarioCrearDto, Propietario>();

        // DTO de entrada -> Entidad existente (PUT)
        CreateMap<PropietarioActualizarDto, Propietario>();

        // Entidad -> DTO de lectura (lo que devuelve la API)
        CreateMap<Mascota, MascotaDto>()
            .ForMember(destino => destino.Propietario,
                       opciones => opciones.MapFrom(origen => origen.Propietario.Nombres));

        // DTO de entrada -> Entidad (POST)
        CreateMap<MascotaCrearDto, Mascota>();

        // DTO de entrada -> Entidad existente (PUT)
        CreateMap<MascotaActualizarDto, Mascota>();

        // Entidad -> DTO de lectura (lo que devuelve el registro, sin el hash de la clave)
        CreateMap<Usuario, UsuarioDto>();

        // DTO de entrada -> Entidad (registro): el hash y el rol los asigna el servicio, no el cliente
        CreateMap<RegistroDto, Usuario>()
            .ForMember(destino => destino.ClaveHash, opciones => opciones.Ignore())
            .ForMember(destino => destino.Rol, opciones => opciones.Ignore());
    }
}
