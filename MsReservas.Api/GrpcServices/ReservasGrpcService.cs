using System.Text.Json;
using Atracciones.Grpc;
using Atracciones.Shared.Models;
using Grpc.Core;
using MsReservas.Api.Dtos;
using MsReservas.Api.Services;

namespace MsReservas.Api.GrpcServices;

public class ReservasGrpcService : ReservasGrpc.ReservasGrpcBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ReservaService _reservaService;

    public ReservasGrpcService(ReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    public override async Task<JsonReply> Crear(JsonRequest request, ServerCallContext context)
    {
        var body = JsonSerializer.Deserialize<CrearReservaRequest>(request.Json, JsonOptions) ?? new CrearReservaRequest();
        var result = await _reservaService.CrearAsync(body, context.CancellationToken);
        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Reserva creada correctamente."));
    }

    public override async Task<JsonReply> Listar(ListarReservasRequest request, ServerCallContext context)
    {
        var result = Guid.TryParse(request.ClienteGuid, out var clienteGuid)
            ? await _reservaService.ListarPorClienteAsync(clienteGuid, context.CancellationToken)
            : await _reservaService.ListarAsync(context.CancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ReservaResponse>>.Ok(result, "Consulta exitosa."));
    }

    public override async Task<JsonReply> Obtener(GuidRequest request, ServerCallContext context)
    {
        var result = await _reservaService.ObtenerPorGuidAsync(Guid.Parse(request.Guid), context.CancellationToken);
        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Consulta exitosa."));
    }

    public override async Task<JsonReply> ConfirmarPago(GuidRequest request, ServerCallContext context)
    {
        var result = await _reservaService.ConfirmarPagoAsync(Guid.Parse(request.Guid), context.CancellationToken);
        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Pago confirmado correctamente."));
    }

    private static JsonReply Ok<T>(T value)
    {
        return new JsonReply
        {
            StatusCode = StatusCodes.Status200OK,
            Json = JsonSerializer.Serialize(value, JsonOptions)
        };
    }
}
