namespace MsReservas.Api.Dtos;

public class CrearReservaRequest
{
    public Guid ClienteGuid { get; set; }
    public Guid HorarioGuid { get; set; }
    public string? OrigenCanal { get; set; }
    public decimal? ValorIva { get; set; }
    public IReadOnlyCollection<CrearReservaDetalleRequest> Detalles { get; set; } = Array.Empty<CrearReservaDetalleRequest>();
}

public class CrearReservaDetalleRequest
{
    public Guid TicketGuid { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class CancelarReservaRequest
{
    public string Motivo { get; set; } = null!;
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
