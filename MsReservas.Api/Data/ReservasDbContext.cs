using Microsoft.EntityFrameworkCore;
using MsReservas.Api.Data.Entities;

namespace MsReservas.Api.Data;

public class ReservasDbContext : DbContext
{
    public ReservasDbContext(DbContextOptions<ReservasDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReservaEntity> Reservas => Set<ReservaEntity>();
    public DbSet<ReservaDetalleEntity> ReservaDetalles => Set<ReservaDetalleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReservaEntity>(builder =>
        {
            builder.ToTable("reservas");
            builder.HasKey(x => x.RevId);
            builder.Property(x => x.RevId).HasColumnName("rev_id").ValueGeneratedOnAdd();
            builder.Property(x => x.RevGuid).HasColumnName("rev_guid").IsRequired();
            builder.Property(x => x.RevCodigo).HasColumnName("rev_codigo").HasMaxLength(20).IsRequired();
            builder.Property(x => x.CliGuid).HasColumnName("cli_guid").IsRequired();
            builder.Property(x => x.HorGuid).HasColumnName("hor_guid").IsRequired();
            builder.Property(x => x.RevFechaReservaUtc).HasColumnName("rev_fecha_reserva_utc").IsRequired();
            builder.Property(x => x.RevSubtotal).HasColumnName("rev_subtotal").HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.RevValorIva).HasColumnName("rev_valor_iva").HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.RevTotal).HasColumnName("rev_total").HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.RevOrigenCanal).HasColumnName("rev_origen_canal").HasMaxLength(50);
            builder.Property(x => x.RevUsuarioIngreso).HasColumnName("rev_usuario_ingreso").HasMaxLength(100).IsRequired();
            builder.Property(x => x.RevIpIngreso).HasColumnName("rev_ip_ingreso").HasMaxLength(45).IsRequired();
            builder.Property(x => x.RevFechaMod).HasColumnName("rev_fecha_mod");
            builder.Property(x => x.RevUsuarioMod).HasColumnName("rev_usuario_mod").HasMaxLength(100);
            builder.Property(x => x.RevIpMod).HasColumnName("rev_ip_mod").HasMaxLength(45);
            builder.Property(x => x.RevFechaCancelacion).HasColumnName("rev_fecha_cancelacion");
            builder.Property(x => x.RevUsuarioCancelacion).HasColumnName("rev_usuario_cancelacion").HasMaxLength(100);
            builder.Property(x => x.RevIpCancelacion).HasColumnName("rev_ip_cancelacion").HasMaxLength(45);
            builder.Property(x => x.RevMotivoCancelacion).HasColumnName("rev_motivo_cancelacion").HasMaxLength(300);
            builder.Property(x => x.RevEstado).HasColumnName("rev_estado").HasMaxLength(1).IsRequired();
            builder.HasIndex(x => x.RevGuid).IsUnique();
            builder.HasIndex(x => x.RevCodigo).IsUnique();
            builder.HasIndex(x => x.CliGuid);
            builder.HasIndex(x => x.HorGuid);
        });

        modelBuilder.Entity<ReservaDetalleEntity>(builder =>
        {
            builder.ToTable("reserva_detalle");
            builder.HasKey(x => x.RdetId);
            builder.Property(x => x.RdetId).HasColumnName("rdet_id").ValueGeneratedOnAdd();
            builder.Property(x => x.RdetGuid).HasColumnName("rdet_guid").IsRequired();
            builder.Property(x => x.RevId).HasColumnName("rev_id").IsRequired();
            builder.Property(x => x.TckGuid).HasColumnName("tck_guid").IsRequired();
            builder.Property(x => x.RdetCantidad).HasColumnName("rdet_cantidad").IsRequired();
            builder.Property(x => x.RdetPrecioUnit).HasColumnName("rdet_precio_unit").HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.RdetSubtotal).HasColumnName("rdet_subtotal").HasPrecision(10, 2).IsRequired();
            builder.Property(x => x.RdetFechaIngreso).HasColumnName("rdet_fecha_ingreso").IsRequired();
            builder.Property(x => x.RdetUsuarioIngreso).HasColumnName("rdet_usuario_ingreso").HasMaxLength(100).IsRequired();
            builder.Property(x => x.RdetIpIngreso).HasColumnName("rdet_ip_ingreso").HasMaxLength(45).IsRequired();
            builder.Property(x => x.RdetFechaEliminacion).HasColumnName("rdet_fecha_eliminacion");
            builder.Property(x => x.RdetUsuarioEliminacion).HasColumnName("rdet_usuario_eliminacion").HasMaxLength(100);
            builder.Property(x => x.RdetIpEliminacion).HasColumnName("rdet_ip_eliminacion").HasMaxLength(45);
            builder.Property(x => x.RdetEstado).HasColumnName("rdet_estado").HasMaxLength(1).IsRequired();
            builder.HasIndex(x => x.RdetGuid).IsUnique();
            builder.HasIndex(x => x.TckGuid);
            builder.HasIndex(x => new { x.RevId, x.TckGuid }).IsUnique();
            builder.HasOne(x => x.Reserva).WithMany(x => x.Detalles).HasForeignKey(x => x.RevId);
        });
    }
}
