using Atracciones.Shared.Exceptions;
using Atracciones.Shared.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MsReservas.Api.Data;
using MsReservas.Api.Data.Entities;
using MsReservas.Api.Dtos;

namespace MsReservas.Api.Services;

public class ReservaService
{
    private readonly ReservasDbContext _context;
    private readonly IEventBusPublisher _eventBus;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReservaService(
        ReservasDbContext context,
        IEventBusPublisher eventBus,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _eventBus = eventBus;
        _httpContextAccessor = httpContextAccessor;
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
            CliGuid = request.ClienteGuid == Guid.Empty ? Guid.NewGuid() : request.ClienteGuid,
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
        await PublishReservaEventAsync("reservas.reserva.creada", reserva, new
        {
            cupos_descontados_por_gateway = true
        }, cancellationToken);

        return ToResponse(reserva);
    }

    public async Task<ReservaResponse> ConfirmarPagoAsync(Guid guid, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas
            .Include(x => x.Detalles.Where(d => d.RdetEstado == "A"))
            .FirstOrDefaultAsync(x => x.RevGuid == guid, cancellationToken);

        if (reserva is null)
            throw new NotFoundException("No se encontro la reserva.");

        if (reserva.RevEstado == "P")
            throw new ConflictException("La reserva ya fue pagada.");

        if (reserva.RevEstado != "A")
            throw new ConflictException("La reserva no esta en estado pendiente de pago.");

        reserva.RevFechaMod = DateTimeOffset.UtcNow;
        reserva.RevUsuarioMod = "api";
        reserva.RevIpMod = "127.0.0.1";
        reserva.RevEstado = "P";

        await _context.SaveChangesAsync(cancellationToken);
        await PublishReservaEventAsync("reservas.reserva.pagada", reserva, new
        {
            nombre_receptor = "Consumidor",
            correo_receptor = "sin-correo@booking.local",
            factura_generada_por_gateway = false
        }, cancellationToken);

        return ToResponse(reserva);
    }

    public async Task<PagoConfirmadoResponse> ConfirmarPagoBookingAsync(
        Guid guid,
        ConfirmarPagoRequest request,
        CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas
            .Include(x => x.Detalles.Where(d => d.RdetEstado == "A"))
            .FirstOrDefaultAsync(x => x.RevGuid == guid, cancellationToken);

        if (reserva is null)
            throw new NotFoundException("No se encontro la reserva.");

        if (reserva.RevEstado == "P")
            throw new ConflictException("La reserva ya fue pagada.");

        if (reserva.RevEstado != "A")
            throw new ConflictException("La reserva no esta en estado pendiente de pago.");

        reserva.RevFechaMod = DateTimeOffset.UtcNow;
        reserva.RevUsuarioMod = "booking";
        reserva.RevIpMod = "127.0.0.1";
        reserva.RevEstado = "P";

        await _context.SaveChangesAsync(cancellationToken);

        var nombre = string.IsNullOrWhiteSpace(request.NombreReceptor)
            ? "Consumidor"
            : request.NombreReceptor.Trim();

        var correo = string.IsNullOrWhiteSpace(request.CorreoReceptor)
            ? "sin-correo@booking.local"
            : request.CorreoReceptor.Trim();

        var facNumero = GenerateInvoiceNumber();
        await PublishReservaEventAsync("reservas.reserva.pagada", reserva, new
        {
            fac_numero = facNumero,
            nombre_receptor = nombre,
            apellido_receptor = request.ApellidoReceptor,
            correo_receptor = correo,
            telefono_receptor = request.TelefonoReceptor,
            observacion = request.Observacion,
            factura_generada_por_gateway = true
        }, cancellationToken);

        return new PagoConfirmadoResponse
        {
            FacGuid = Guid.NewGuid(),
            FacNumero = facNumero,
            RevCodigo = reserva.RevCodigo,
            Total = reserva.RevTotal,
            FechaEmision = DateTimeOffset.UtcNow,
            NombreReceptor = nombre,
            CorreoReceptor = correo
        };
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
        await PublishReservaEventAsync("reservas.reserva.cancelada", reserva, new
        {
            motivo = request.Motivo.Trim(),
            cupos_liberados_por_gateway = true
        }, cancellationToken);

        return true;
    }

    private static void ValidateCrear(CrearReservaRequest request)
    {
        var errors = new List<string>();

        if (request.ClienteGuid == Guid.Empty && request.ClienteInvitado is null)
            errors.Add("Debe enviar clienteGuid o cliente_invitado.");

        if (request.ClienteInvitado is not null)
        {
            if (string.IsNullOrWhiteSpace(request.ClienteInvitado.TipoIdentificacion))
                errors.Add("cliente_invitado.tipo_identificacion es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.ClienteInvitado.NumeroIdentificacion))
                errors.Add("cliente_invitado.numero_identificacion es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.ClienteInvitado.Correo))
                errors.Add("cliente_invitado.correo es obligatorio.");
        }

        if (request.HorarioGuid == Guid.Empty)
            errors.Add("El hor_guid es obligatorio.");

        if (request.Detalles.Count == 0)
            errors.Add("Debe agregar al menos una linea de reserva.");

        foreach (var detalle in request.Detalles)
        {
            if (detalle.TicketGuid == Guid.Empty)
                errors.Add("Cada linea debe tener tck_guid.");

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

    private static string GenerateInvoiceNumber()
    {
        return $"FAC{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}";
    }

    private async Task PublishReservaEventAsync(
        string routingKey,
        ReservaEntity reserva,
        object extra,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            rev_guid = reserva.RevGuid,
            rev_codigo = reserva.RevCodigo,
            cli_guid = reserva.CliGuid,
            hor_guid = reserva.HorGuid,
            estado = reserva.RevEstado,
            origen_canal = reserva.RevOrigenCanal,
            subtotal = reserva.RevSubtotal,
            valor_iva = reserva.RevValorIva,
            total = reserva.RevTotal,
            fecha_reserva_utc = reserva.RevFechaReservaUtc,
            detalles = reserva.Detalles
                .Where(x => x.RdetEstado == "A")
                .Select(x => new
                {
                    rdet_guid = x.RdetGuid,
                    tck_guid = x.TckGuid,
                    cantidad = x.RdetCantidad,
                    precio_unitario = x.RdetPrecioUnit,
                    subtotal = x.RdetSubtotal
                })
                .ToList(),
            metadata = extra
        };

        await _eventBus.PublishAsync(routingKey, payload, GetCorrelationId(), cancellationToken);
    }

    private string? GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context?.TraceIdentifier;
    }
}
