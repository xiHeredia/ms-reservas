using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Atracciones.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MsReservas.Api.Dtos;
using MsReservas.Api.Services;

namespace MsReservas.Api.Controllers;

[ApiController]
[Route("api/v1/reservas")]
public class ReservasController : ControllerBase
{
    private readonly ReservaService _reservaService;

    public ReservasController(ReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var clienteGuid = GetClienteGuidFromToken();
        var result = clienteGuid is null || IsStaff()
            ? await _reservaService.ListarAsync(cancellationToken)
            : await _reservaService.ListarPorClienteAsync(clienteGuid.Value, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ReservaResponse>>.Ok(result, "Consulta exitosa."));
    }

    [HttpGet("{guid:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerPorGuid(Guid guid, CancellationToken cancellationToken)
    {
        var result = await _reservaService.ObtenerPorGuidAsync(guid, cancellationToken);

        var clienteGuid = GetClienteGuidFromToken();
        if (clienteGuid is not null && !IsStaff() && result.ClienteGuid != clienteGuid.Value)
            return Forbid();

        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Consulta exitosa."));
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Crear([FromBody] CrearReservaRequest request, CancellationToken cancellationToken)
    {
        var result = await _reservaService.CrearAsync(request, cancellationToken);
        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Reserva creada correctamente."));
    }

    [HttpPost("{guid:guid}/confirmar-pago")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmarPago(Guid guid, CancellationToken cancellationToken)
    {
        var result = await _reservaService.ConfirmarPagoAsync(guid, cancellationToken);
        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Pago confirmado correctamente."));
    }

    [HttpPost("{guid:guid}/pagos/confirmacion")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmarPagoBooking(Guid guid, CancellationToken cancellationToken)
    {
        var result = await _reservaService.ConfirmarPagoAsync(guid, cancellationToken);
        return Ok(ApiResponse<ReservaResponse>.Ok(result, "Pago confirmado correctamente."));
    }

    [HttpPut("{guid:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Actualizar(Guid guid, [FromBody] ActualizarReservaRequest request, CancellationToken cancellationToken)
    {
        var ok = await _reservaService.ActualizarAsync(guid, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok, "Reserva actualizada correctamente."));
    }

    [HttpPost("{guid:guid}/cancelar")]
    [HttpPatch("{guid:guid}/cancelar")]
    [AllowAnonymous]
    public async Task<IActionResult> Cancelar(Guid guid, [FromBody] CancelarReservaRequest request, CancellationToken cancellationToken)
    {
        var ok = await _reservaService.CancelarAsync(guid, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(ok, "Reserva cancelada correctamente."));
    }

    private Guid? GetClienteGuidFromToken()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var guid) ? guid : null;
    }

    private bool IsStaff()
    {
        return User.IsInRole("ADMIN") || User.IsInRole("OPERADOR");
    }
}
