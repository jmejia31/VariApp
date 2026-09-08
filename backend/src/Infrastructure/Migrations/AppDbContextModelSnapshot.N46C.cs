using System;
using InventoryApp.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace InventoryApp.Infrastructure.Migrations
{
    partial class AppDbContextModelSnapshot
    {
        /// <summary>
        /// N4.6.C — alinea el modelo efectivo del snapshot EF con CuentaContable sin reescribir
        /// el snapshot canónico histórico. Mantiene el mismo patrón aditivo usado por N4.3.C.
        /// N4.11.C extiende el mismo snapshot efectivo con CentroCosto para evitar drift de modelo.
        /// </summary>
        private static void ApplyN46CModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity("InventoryApp.Domain.Entities.CuentaContable", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                b.Property<bool>("AceptaMovimientos")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(true);

                b.Property<bool>("Activa")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(true);

                b.Property<string>("ActualizadoPorNombreUsuario")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("ActualizadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<string>("Codigo")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("varchar(50)");

                b.Property<string>("CreadoPorNombreUsuario")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("CreadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<int?>("CuentaPadreId")
                    .HasColumnType("int");

                b.Property<string>("Descripcion")
                    .HasMaxLength(1000)
                    .HasColumnType("varchar(1000)");

                b.Property<DateTime>("FechaActualizacion")
                    .HasColumnType("datetime(6)");

                b.Property<DateTime>("FechaCreacion")
                    .HasColumnType("datetime(6)");

                b.Property<string>("Nombre")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("varchar(200)");

                b.Property<int>("Tipo")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("CuentaPadreId")
                    .HasDatabaseName("IX_CuentasContables_CuentaPadreId");

                b.HasIndex("Tipo", "Activa")
                    .HasDatabaseName("IX_CuentasContables_Tipo_Activa");

                b.HasIndex("Codigo")
                    .IsUnique()
                    .HasDatabaseName("UX_CuentasContables_Codigo");

                b.ToTable("CuentasContables", null, t =>
                {
                    t.HasCheckConstraint("CK_CuentasContables_Codigo", "CHAR_LENGTH(TRIM(`Codigo`)) > 0");
                    t.HasCheckConstraint("CK_CuentasContables_Nombre", "CHAR_LENGTH(TRIM(`Nombre`)) > 0");
                    t.HasCheckConstraint("CK_CuentasContables_Tipo", "`Tipo` BETWEEN 1 AND 6");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.CuentaContable", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.CuentaContable", "CuentaPadre")
                    .WithMany("Subcuentas")
                    .HasForeignKey("CuentaPadreId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("CuentaPadre");
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.CuentaContable", b =>
            {
                b.Navigation("Subcuentas");
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.Contabilidad.CentroCosto", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                b.Property<bool>("Activo")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(true);

                b.Property<string>("ActualizadoPorNombreUsuario")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("ActualizadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<string>("Codigo")
                    .IsRequired()
                    .HasMaxLength(40)
                    .HasColumnType("varchar(40)");

                b.Property<string>("CodigoActivoUnico")
                    .ValueGeneratedOnAddOrUpdate()
                    .HasMaxLength(40)
                    .HasColumnType("varchar(40)")
                    .HasComputedColumnSql("IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", true);

                b.Property<string>("CreadoPorNombreUsuario")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("CreadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<string>("Descripcion")
                    .HasMaxLength(500)
                    .HasColumnType("varchar(500)");

                b.Property<bool>("Eliminado")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(false);

                b.Property<int?>("EliminadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<DateTime>("FechaActualizacion")
                    .HasColumnType("datetime(6)");

                b.Property<DateTime>("FechaCreacion")
                    .HasColumnType("datetime(6)");

                b.Property<DateTime?>("FechaEliminacion")
                    .HasColumnType("datetime(6)");

                b.Property<string>("Nombre")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("SucursalId")
                    .HasColumnType("int");

                b.Property<int>("Tipo")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("CodigoActivoUnico")
                    .IsUnique()
                    .HasDatabaseName("UX_CentrosCosto_Codigo_Activo");

                b.HasIndex("SucursalId")
                    .HasDatabaseName("IX_CentrosCosto_SucursalId");

                b.HasIndex("Tipo", "Activo", "Eliminado")
                    .HasDatabaseName("IX_CentrosCosto_Tipo_Estado");

                b.ToTable("CentrosCosto", null, t =>
                {
                    t.HasCheckConstraint("CK_CentrosCosto_Tipo", "`Tipo` IN (1, 2, 3, 4)");
                    t.HasCheckConstraint("CK_CentrosCosto_Asociacion", "(`Tipo` = 1 AND `SucursalId` IS NOT NULL) OR (`Tipo` <> 1 AND `SucursalId` IS NULL)");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.Contabilidad.CentroCosto", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.Sucursal", "Sucursal")
                    .WithMany()
                    .HasForeignKey("SucursalId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("Sucursal");
            });

            modelBuilder.Entity<CentroCosto>()
                .HasQueryFilter(x => !x.Eliminado);
        }
    }
}
