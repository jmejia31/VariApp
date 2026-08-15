from pathlib import Path

path = Path("backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs")
text = path.read_text(encoding="utf-8-sig")

anchor = 'modelBuilder.Entity("InventoryApp.Domain.Entities.AjusteInventario", b =>'
if anchor not in text:
    raise SystemExit("Anchor compacto no encontrado; abortando sin modificar snapshot.")
if "IX_CompraDetalles_Almacen_Ubicacion" in text:
    raise SystemExit("Delta N1.4.D ya existe; abortando para evitar duplicado.")

anchor_index = text.rfind(anchor)
line_start = text.rfind("\n", 0, anchor_index) + 1

delta = '''            // ERP-N1.4.D — contexto físico histórico de operaciones.
            modelBuilder.Entity("InventoryApp.Domain.Entities.CompraDetalle", b =>
            {
                b.Property<int?>("AlmacenId").HasColumnType("int");
                b.Property<int?>("UbicacionAlmacenId").HasColumnType("int");
                b.HasIndex("AlmacenId", "UbicacionAlmacenId")
                    .HasDatabaseName("IX_CompraDetalles_Almacen_Ubicacion");
                b.ToTable("CompraDetalles", null, t =>
                {
                    t.HasCheckConstraint("CK_CompraDetalles_Ubicacion_RequiereAlmacen", "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.VentaDetalle", b =>
            {
                b.Property<int?>("AlmacenId").HasColumnType("int");
                b.Property<int?>("UbicacionAlmacenId").HasColumnType("int");
                b.HasIndex("AlmacenId", "UbicacionAlmacenId")
                    .HasDatabaseName("IX_VentaDetalles_Almacen_Ubicacion");
                b.ToTable("VentaDetalles", null, t =>
                {
                    t.HasCheckConstraint("CK_VentaDetalles_Ubicacion_RequiereAlmacen", "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.ConsumoInsumoDetalle", b =>
            {
                b.Property<int?>("AlmacenId").HasColumnType("int");
                b.Property<int?>("UbicacionAlmacenId").HasColumnType("int");
                b.HasIndex("AlmacenId", "UbicacionAlmacenId")
                    .HasDatabaseName("IX_ConsumoInsumoDetalles_Almacen_Ubicacion");
                b.ToTable("ConsumoInsumoDetalles", null, t =>
                {
                    t.HasCheckConstraint("CK_ConsumoInsumoDetalles_Ubicacion_RequiereAlmacen", "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.MovimientoInventario", b =>
            {
                b.Property<int?>("AlmacenId").HasColumnType("int");
                b.Property<int?>("UbicacionAlmacenId").HasColumnType("int");
                b.HasIndex("AlmacenId", "UbicacionAlmacenId")
                    .HasDatabaseName("IX_MovimientosInventario_Almacen_Ubicacion");
                b.ToTable("MovimientosInventario", null, t =>
                {
                    t.HasCheckConstraint("CK_MovimientosInventario_Ubicacion_RequiereAlmacen", "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.AjusteInventarioDetalle", b =>
            {
                b.Property<int?>("AlmacenId").HasColumnType("int");
                b.Property<int?>("UbicacionAlmacenId").HasColumnType("int");
                b.HasIndex("AlmacenId", "UbicacionAlmacenId")
                    .HasDatabaseName("IX_AjusteInventarioDetalles_Almacen_Ubicacion");
                b.ToTable("AjusteInventarioDetalles", null, t =>
                {
                    t.HasCheckConstraint("CK_AjusteInventarioDetalles_Ubicacion_RequiereAlmacen", "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");
                });
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.CompraDetalle", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.Almacen", "Almacen")
                    .WithMany().HasForeignKey("AlmacenId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_CompraDetalles_Almacenes_AlmacenId_N14");
                b.HasOne("InventoryApp.Domain.Entities.UbicacionAlmacen", "UbicacionAlmacen")
                    .WithMany().HasForeignKey("AlmacenId", "UbicacionAlmacenId")
                    .HasPrincipalKey("AlmacenId", "Id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_CompraDetalles_Ubicacion_MismoAlmacen_N14");
                b.Navigation("Almacen");
                b.Navigation("UbicacionAlmacen");
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.VentaDetalle", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.Almacen", "Almacen")
                    .WithMany().HasForeignKey("AlmacenId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_VentaDetalles_Almacenes_AlmacenId_N14");
                b.HasOne("InventoryApp.Domain.Entities.UbicacionAlmacen", "UbicacionAlmacen")
                    .WithMany().HasForeignKey("AlmacenId", "UbicacionAlmacenId")
                    .HasPrincipalKey("AlmacenId", "Id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_VentaDetalles_Ubicacion_MismoAlmacen_N14");
                b.Navigation("Almacen");
                b.Navigation("UbicacionAlmacen");
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.ConsumoInsumoDetalle", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.Almacen", "Almacen")
                    .WithMany().HasForeignKey("AlmacenId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ConsumoInsumoDetalles_Almacenes_AlmacenId_N14");
                b.HasOne("InventoryApp.Domain.Entities.UbicacionAlmacen", "UbicacionAlmacen")
                    .WithMany().HasForeignKey("AlmacenId", "UbicacionAlmacenId")
                    .HasPrincipalKey("AlmacenId", "Id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ConsumoInsumoDetalles_Ubicacion_MismoAlmacen_N14");
                b.Navigation("Almacen");
                b.Navigation("UbicacionAlmacen");
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.MovimientoInventario", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.Almacen", "Almacen")
                    .WithMany().HasForeignKey("AlmacenId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_MovimientosInventario_Almacenes_AlmacenId_N14");
                b.HasOne("InventoryApp.Domain.Entities.UbicacionAlmacen", "UbicacionAlmacen")
                    .WithMany().HasForeignKey("AlmacenId", "UbicacionAlmacenId")
                    .HasPrincipalKey("AlmacenId", "Id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_MovimientosInventario_Ubicacion_MismoAlmacen_N14");
                b.Navigation("Almacen");
                b.Navigation("UbicacionAlmacen");
            });

            modelBuilder.Entity("InventoryApp.Domain.Entities.AjusteInventarioDetalle", b =>
            {
                b.HasOne("InventoryApp.Domain.Entities.Almacen", "Almacen")
                    .WithMany().HasForeignKey("AlmacenId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AjusteInventarioDetalles_Almacenes_AlmacenId_N14");
                b.HasOne("InventoryApp.Domain.Entities.UbicacionAlmacen", "UbicacionAlmacen")
                    .WithMany().HasForeignKey("AlmacenId", "UbicacionAlmacenId")
                    .HasPrincipalKey("AlmacenId", "Id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AjusteInventarioDetalles_Ubicacion_MismoAlmacen_N14");
                b.Navigation("Almacen");
                b.Navigation("UbicacionAlmacen");
            });

'''

text = text[:line_start] + delta + text[line_start:]
path.write_text(text, encoding="utf-8")
print(f"patched snapshot lines={len(text.splitlines())}")
