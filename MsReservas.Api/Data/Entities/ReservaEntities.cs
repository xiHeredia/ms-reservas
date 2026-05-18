namespace MsReservas.Api.Data.Entities;

public class ReservaEntity
{
    public int RevId { get; set; }
    public Guid RevGuid { get; set; }
    public string RevCodigo { get; set; } = null!;
    public Guid CliGuid { get; set; }
    public Guid HorGuid { get; set; }
    public DateTimeOffset RevFechaReservaUtc { get; set; }
    public decimal RevSubtotal { get; set; }
    public decimal RevValorIva { get; set; }
    public decimal RevTotal { get; set; }
    public string? RevOrigenCanal { get; set; }
    public string RevUsuarioIngreso { get; set; } = null!;
    public string RevIpIngreso { get; set; } = null!;
    public DateTimeOffset? RevFechaMod { get; set; }
    public string? RevUsuarioMod { get; set; }
    public string? RevIpMod { get; set; }
    public DateTimeOffset? RevFechaCancelacion { get; set; }
    public string? RevUsuarioCancelacion { get; set; }
    public string? RevIpCancelacion { get; set; }
    public string? RevMotivoCancelacion { get; set; }
    public string RevEstado { get; set; } = "A";
    public ICollection<ReservaDetalleEntity> Detalles { get; set; } = new List<ReservaDetalleEntity>();
}

public class ReservaDetalleEntity
{
    public int RdetId { get; set; }
    public Guid RdetGuid { get; set; }
    public int RevId { get; set; }
    public Guid TckGuid { get; set; }
    public int RdetCantidad { get; set; }
    public decimal RdetPrecioUnit { get; set; }
    public decimal RdetSubtotal { get; set; }
    public DateTimeOffset RdetFechaIngreso { get; set; }
    public string RdetUsuarioIngreso { get; set; } = null!;
    public string RdetIpIngreso { get; set; } = null!;
    public DateTimeOffset? RdetFechaEliminacion { get; set; }
    public string? RdetUsuarioEliminacion { get; set; }
    public string? RdetIpEliminacion { get; set; }
    public string RdetEstado { get; set; } = "A";
    public ReservaEntity Reserva { get; set; } = null!;
}
