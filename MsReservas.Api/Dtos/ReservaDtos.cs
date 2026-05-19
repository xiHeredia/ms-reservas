namespace MsReservas.Api.Dtos;

using System.Text.Json.Serialization;

public class CrearReservaRequest
{
    public Guid ClienteGuid { get; set; }
    public Guid AtraccionGuid { get; set; }
    public Guid HorarioGuid { get; set; }
    public string? OrigenCanal { get; set; }
    public decimal? ValorIva { get; set; }
    public IReadOnlyCollection<CrearReservaDetalleRequest> Detalles { get; set; } = Array.Empty<CrearReservaDetalleRequest>();
    public ClienteInvitadoRequest? ClienteInvitado { get; set; }

    [JsonPropertyName("cliente_guid")]
    public Guid ClienteGuidSnake
    {
        get => ClienteGuid;
        set => ClienteGuid = value;
    }

    [JsonPropertyName("at_guid")]
    public Guid AtraccionGuidSnake
    {
        get => AtraccionGuid;
        set => AtraccionGuid = value;
    }

    [JsonPropertyName("hor_guid")]
    public Guid HorarioGuidSnake
    {
        get => HorarioGuid;
        set => HorarioGuid = value;
    }

    public Guid HorarioId
    {
        get => HorarioGuid;
        set => HorarioGuid = value;
    }

    [JsonPropertyName("origen_canal")]
    public string? OrigenCanalSnake
    {
        get => OrigenCanal;
        set => OrigenCanal = value;
    }

    [JsonPropertyName("valor_iva")]
    public decimal? ValorIvaSnake
    {
        get => ValorIva;
        set => ValorIva = value;
    }

    [JsonPropertyName("lineas")]
    public IReadOnlyCollection<CrearReservaDetalleRequest> Lineas
    {
        get => Detalles;
        set => Detalles = value ?? Array.Empty<CrearReservaDetalleRequest>();
    }

    public IReadOnlyCollection<CrearReservaDetalleRequest> Tickets
    {
        get => Detalles;
        set => Detalles = value ?? Array.Empty<CrearReservaDetalleRequest>();
    }

    [JsonPropertyName("cliente_invitado")]
    public ClienteInvitadoRequest? ClienteInvitadoSnake
    {
        get => ClienteInvitado;
        set => ClienteInvitado = value;
    }
}

public class CrearReservaDetalleRequest
{
    public Guid TicketGuid { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("tck_guid")]
    public Guid TicketGuidSnake
    {
        get => TicketGuid;
        set => TicketGuid = value;
    }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitarioSnake
    {
        get => PrecioUnitario;
        set => PrecioUnitario = value;
    }
}

public class ClienteInvitadoRequest
{
    public string? TipoIdentificacion { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }

    [JsonPropertyName("tipo_identificacion")]
    public string? TipoIdentificacionSnake
    {
        get => TipoIdentificacion;
        set => TipoIdentificacion = value;
    }

    [JsonPropertyName("numero_identificacion")]
    public string? NumeroIdentificacionSnake
    {
        get => NumeroIdentificacion;
        set => NumeroIdentificacion = value;
    }
}

public class CancelarReservaRequest
{
    public string Motivo { get; set; } = null!;
}

public class ConfirmarPagoRequest
{
    public string? NombreReceptor { get; set; }
    public string? ApellidoReceptor { get; set; }
    public string? CorreoReceptor { get; set; }
    public string? TelefonoReceptor { get; set; }
    public string? Observacion { get; set; }

    [JsonPropertyName("nombre_receptor")]
    public string? NombreReceptorSnake
    {
        get => NombreReceptor;
        set => NombreReceptor = value;
    }

    [JsonPropertyName("apellido_receptor")]
    public string? ApellidoReceptorSnake
    {
        get => ApellidoReceptor;
        set => ApellidoReceptor = value;
    }

    [JsonPropertyName("correo_receptor")]
    public string? CorreoReceptorSnake
    {
        get => CorreoReceptor;
        set => CorreoReceptor = value;
    }

    [JsonPropertyName("telefono_receptor")]
    public string? TelefonoReceptorSnake
    {
        get => TelefonoReceptor;
        set => TelefonoReceptor = value;
    }
}

public class PagoConfirmadoResponse
{
    public Guid FacGuid { get; set; }
    public string FacNumero { get; set; } = null!;
    public string RevCodigo { get; set; } = null!;
    public decimal Total { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTimeOffset FechaEmision { get; set; }
    public string Estado { get; set; } = "E";
    public string NombreReceptor { get; set; } = null!;
    public string CorreoReceptor { get; set; } = null!;

    [JsonPropertyName("fac_guid")]
    public Guid FacGuidSnake => FacGuid;

    [JsonPropertyName("fac_numero")]
    public string FacNumeroSnake => FacNumero;

    [JsonPropertyName("rev_codigo")]
    public string RevCodigoSnake => RevCodigo;

    [JsonPropertyName("fecha_emision")]
    public DateTimeOffset FechaEmisionSnake => FechaEmision;

    [JsonPropertyName("nombre_receptor")]
    public string NombreReceptorSnake => NombreReceptor;

    [JsonPropertyName("correo_receptor")]
    public string CorreoReceptorSnake => CorreoReceptor;
}

public class ActualizarReservaRequest
{
    public Guid ClienteGuid { get; set; }
    public Guid ClienteId { get; set; }
    public Guid HorarioGuid { get; set; }
    public Guid HorarioId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal? ValorIva { get; set; }
    public decimal? Total { get; set; }
    public string? OrigenCanal { get; set; }
}

public class ReservaResponse
{
    public Guid Guid { get; set; }
    public string Codigo { get; set; } = null!;
    public Guid ClienteGuid { get; set; }
    public Guid HorarioGuid { get; set; }
    public DateTimeOffset FechaReservaUtc { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ValorIva { get; set; }
    public decimal Total { get; set; }
    public string? OrigenCanal { get; set; }
    public string Estado { get; set; } = null!;
    public IReadOnlyCollection<ReservaDetalleResponse> Detalles { get; set; } = Array.Empty<ReservaDetalleResponse>();
}

public class ReservaDetalleResponse
{
    public Guid Guid { get; set; }
    public Guid TicketGuid { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
