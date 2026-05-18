using Atracciones.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using MsReservas.Api.Data;
using MsReservas.Api.Data.Entities;
using MsReservas.Api.Dtos;

namespace MsReservas.Api.Services;

public class ReservaService
{
    private readonly ReservasDbContext _context;

    public ReservaService(ReservasDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ReservaResponse>> ListarAsync(CancellationToken cancellationToken)
    {
        return await ListarInternalAsync(clienteGuid: null, cancellationToken);
    }

    public async Task<IReadOnlyList<ReservaResponse>> ListarPorClienteAsync(Guid clienteGuid, CancellationToken cancellationToken)
    {
        if (clienteGuid == Guid.Empty)
            throw new ValidationException("El clienteGuid es obligatorio.");

        return await ListarInternalAsync(clienteGuid, cancellationToken);
    }

    private async Task<IReadOnlyList<ReservaResponse>> ListarInternalAsync(Guid? clienteGuid, CancellationToken cancellationToken)
    {
        var query = _context.Reservas
            .AsNoTracking()
            .Include(x => x.Detalles.Where(d => d.RdetEstado == "A"))
            .AsQueryable();

        if (clienteGuid is not null)
            query = query.Where(x => x.CliGuid == clienteGuid.Value);

        var reservas = await query
            .OrderByDescending(x => x.RevFechaReservaUtc)
            .ToListAsync(cancellationToken);

        return reservas.Select(ToResponse).ToList();
    }

    public async Task<ReservaResponse> ObtenerPorGuidAsync(Guid guid, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas
            .AsNoTracking()
            .Include(x => x.Detalles.Where(d => d.RdetEstado == "A"))
            .FirstOrDefaultAsync(x => x.RevGuid == guid, cancellationToken);

        if (reserva is null)
            throw new NotFoundException("No se encontro la reserva.");

        return ToResponse(reserva);
    }

    public async Task<ReservaResponse> CrearAsync(CrearReservaRequest request, CancellationToken cancellationToken)
    {
        ValidateCrear(request);

        var subtotal = request.Detalles.Sum(x => x.Cantidad * x.PrecioUnitario);
        var iva = request.ValorIva ?? Math.Round(subtotal * 0.12m, 2);
        var total = subtotal + iva;

        var reserva = new ReservaEntity
        {
            RevGuid = Guid.NewGuid(),
            RevCodigo = GenerateCode(),
            CliGuid = request.ClienteGuid,
            HorGuid = request.HorarioGuid,
            RevFechaReservaUtc = DateTimeOffset.UtcNow,
            RevSubtotal = subtotal,
            RevValorIva = iva,
            RevTotal = total,
            RevOrigenCanal = request.OrigenCanal ?? "BOOKING",
            RevUsuarioIngreso = "api",
            RevIpIngreso = "127.0.0.1",
            RevEstado = "A"
        };

        foreach (var detalle in request.Detalles)
        {
            reserva.Detalles.Add(new ReservaDetalleEntity
            {
                RdetGuid = Guid.NewGuid(),
                TckGuid = detalle.TicketGuid,
                RdetCantidad = detalle.Cantidad,
                RdetPrecioUnit = detalle.PrecioUnitario,
                RdetSubtotal = detalle.Cantidad * detalle.PrecioUnitario,
                RdetFechaIngreso = DateTimeOffset.UtcNow,
                RdetUsuarioIngreso = "api",
                RdetIpIngreso = "127.0.0.1",
                RdetEstado = "A"
            });
        }

        await _context.Reservas.AddAsync(reserva, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(reserva);
    }

    public async Task<ReservaResponse> ConfirmarPagoAsync(Guid guid, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas
            .Include(x => x.Detalles.Where(d => d.RdetEstado == "A"))
            .FirstOrDefaultAsync(x => x.RevGuid == guid && x.RevEstado == "A", cancellationToken);

        if (reserva is null)
            throw new NotFoundException("No se encontro la reserva activa.");

        reserva.RevFechaMod = DateTimeOffset.UtcNow;
        reserva.RevUsuarioMod = "api";
        reserva.RevIpMod = "127.0.0.1";

        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(reserva);
    }

    public async Task<bool> ActualizarAsync(Guid guid, ActualizarReservaRequest request, CancellationToken cancellationToken)
    {
        var clienteGuid = request.ClienteGuid != Guid.Empty ? request.ClienteGuid : request.ClienteId;
        var horarioGuid = request.HorarioGuid != Guid.Empty ? request.HorarioGuid : request.HorarioId;

        if (clienteGuid == Guid.Empty)
            throw new ValidationException("El clienteGuid es obligatorio.");

        if (horarioGuid == Guid.Empty)
            throw new ValidationException("El horarioGuid es obligatorio.");

        if (request.Subtotal < 0)
            throw new ValidationException("El subtotal no puede ser negativo.");

        if (request.ValorIva is < 0 || request.Total is < 0)
            throw new ValidationException("Los valores de IVA y total no pueden ser negativos.");

        var reserva = await _context.Reservas.FirstOrDefaultAsync(
            x => x.RevGuid == guid && x.RevEstado == "A",
            cancellationToken);

        if (reserva is null)
            throw new NotFoundException("No se encontro la reserva activa.");

        var valorIva = request.ValorIva ?? Math.Round(request.Subtotal * 0.12m, 2);
        var total = request.Total ?? request.Subtotal + valorIva;

        reserva.CliGuid = clienteGuid;
        reserva.HorGuid = horarioGuid;
        reserva.RevSubtotal = request.Subtotal;
        reserva.RevValorIva = valorIva;
        reserva.RevTotal = total;
        reserva.RevOrigenCanal = string.IsNullOrWhiteSpace(request.OrigenCanal) ? reserva.RevOrigenCanal : request.OrigenCanal.Trim();
        reserva.RevFechaMod = DateTimeOffset.UtcNow;
        reserva.RevUsuarioMod = "api";
        reserva.RevIpMod = "127.0.0.1";

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelarAsync(Guid guid, CancelarReservaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new ValidationException("El motivo de cancelacion es obligatorio.");

        var reserva = await _context.Reservas
            .Include(x => x.Detalles.Where(d => d.RdetEstado == "A"))
            .FirstOrDefaultAsync(
            x => x.RevGuid == guid && x.RevEstado == "A",
            cancellationToken);

        if (reserva is null)
            throw new NotFoundException("No se encontro la reserva activa.");

        reserva.RevEstado = "C";
        reserva.RevFechaCancelacion = DateTimeOffset.UtcNow;
        reserva.RevUsuarioCancelacion = "api";
        reserva.RevIpCancelacion = "127.0.0.1";
        reserva.RevMotivoCancelacion = request.Motivo.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateCrear(CrearReservaRequest request)
    {
        var errors = new List<string>();

        if (request.ClienteGuid == Guid.Empty)
            errors.Add("El clienteGuid es obligatorio.");

        if (request.HorarioGuid == Guid.Empty)
            errors.Add("El horarioGuid es obligatorio.");

        if (request.Detalles.Count == 0)
            errors.Add("Debe agregar al menos un detalle de reserva.");

        foreach (var detalle in request.Detalles)
        {
            if (detalle.TicketGuid == Guid.Empty)
                errors.Add("Cada detalle debe tener ticketGuid.");

            if (detalle.Cantidad <= 0)
                errors.Add("La cantidad debe ser mayor a cero.");

            if (detalle.PrecioUnitario < 0)
                errors.Add("El precio unitario no puede ser negativo.");
        }

        if (request.ValorIva is < 0)
            errors.Add("El valor IVA no puede ser negativo.");

        if (errors.Count > 0)
            throw new ValidationException("Error de validacion.", errors);
    }

    private static ReservaResponse ToResponse(ReservaEntity entity)
    {
        return new ReservaResponse
        {
            Guid = entity.RevGuid,
            Codigo = entity.RevCodigo,
            ClienteGuid = entity.CliGuid,
            HorarioGuid = entity.HorGuid,
            FechaReservaUtc = entity.RevFechaReservaUtc,
            Subtotal = entity.RevSubtotal,
            ValorIva = entity.RevValorIva,
            Total = entity.RevTotal,
            OrigenCanal = entity.RevOrigenCanal,
            Estado = entity.RevEstado,
            Detalles = entity.Detalles
                .Where(x => x.RdetEstado == "A")
                .Select(x => new ReservaDetalleResponse
                {
                    Guid = x.RdetGuid,
                    TicketGuid = x.TckGuid,
                    Cantidad = x.RdetCantidad,
                    PrecioUnitario = x.RdetPrecioUnit,
                    Subtotal = x.RdetSubtotal
                })
                .ToList()
        };
    }

    private static string GenerateCode()
    {
        return $"RSV{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}
