using Atracciones.Shared.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using MsReservas.Api.Data;
using MsReservas.Api.GrpcServices;
using MsReservas.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.Services.AddAtraccionesApiDefaults(builder.Configuration, "ms-reservas");
builder.Services.AddDbContext<ReservasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReservasDb")));
builder.Services.AddScoped<ReservaService>();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseAtraccionesApiDefaults();
app.MapGrpcService<ReservasGrpcService>();

app.Run();
