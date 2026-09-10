using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class ReportesComprasContractTests
{
    [Fact]
    public void Controller_exige_auth_ruta_y_servicio_inyectado()
    {
        var type = typeof(ReportesComprasController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("compras/reportes", Assert.Single(type.GetCustomAttributes<RouteAttribute>()).Template);

        var ctor = Assert.Single(type.GetConstructors());
        var parameter = Assert.Single(ctor.GetParameters());
        Assert.Equal(typeof(IReporteComprasService), parameter.ParameterType);
    }

    [Fact]
    public void Detalle_exige_get_y_permiso_compras_ver()
    {
        var method = typeof(ReportesComprasController).GetMethod(nameof(ReportesComprasController.GetDetalle))
            ?? throw new InvalidOperationException("No existe GetDetalle.");

        Assert.Equal("detalle", Assert.Single(method.GetCustomAttributes<HttpGetAttribute>()).Template);
        var permiso = Assert.Single(method.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public void DTO_no_inventa_porcentaje_ranking_sla_ni_score()
    {
        var properties = typeof(ReporteComprasDetalleDto).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("VariacionPrecioPorcentual", properties);
        Assert.DoesNotContain("VariacionPrecioPorcentaje", properties);
        Assert.DoesNotContain("RankingProveedor", properties);
        Assert.DoesNotContain("SlaScore", properties);
        Assert.DoesNotContain("CumplimientoScore", properties);
    }

    [Fact]
    public void Filtro_conserva_dimensiones_restrictivas_y_paginacion_acotable()
    {
        var properties = typeof(ReporteComprasFiltroDto).GetProperties().Select(x => x.Name).ToHashSet();
        foreach (var expected in new[]
        {
            nameof(ReporteComprasFiltroDto.DesdeUtc), nameof(ReporteComprasFiltroDto.HastaUtc),
            nameof(ReporteComprasFiltroDto.ProveedorId), nameof(ReporteComprasFiltroDto.ProductoId),
            nameof(ReporteComprasFiltroDto.ProductoVarianteId), nameof(ReporteComprasFiltroDto.EstadoOrden),
            nameof(ReporteComprasFiltroDto.EstadoFactura), nameof(ReporteComprasFiltroDto.EstadoRecepcion),
            nameof(ReporteComprasFiltroDto.EstadoDevolucion), nameof(ReporteComprasFiltroDto.Page),
            nameof(ReporteComprasFiltroDto.PageSize)
        })
        {
            Assert.Contains(expected, properties);
        }
    }
}
