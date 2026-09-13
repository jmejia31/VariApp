using InventoryApp.Domain.Entities;
using Xunit;

namespace InventoryApp.Tests;

public class N610BFeatureFlagsDomainTests
{
    private static readonly DateTime Ahora = new(2026, 9, 13, 1, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Free_ModuloConReglaExplicita_QuedaHabilitado()
    {
        var plan = new Plan("FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1), Ahora.AddDays(29));
        var reglas = new[] { new PlanModulo(10, " inventario ") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 10, plan, reglas, "INVENTARIO", Ahora);

        Assert.True(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.HabilitadoPorReglaExplicita, decision.Motivo);
        Assert.Equal("INVENTARIO", reglas[0].ModuloClave);
    }

    [Fact]
    public void Free_ModuloSinReglaExplicita_FallaCerrado()
    {
        var plan = new Plan("FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1), Ahora.AddDays(29));
        var reglas = new[] { new PlanModulo(10, "INVENTARIO") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 10, plan, reglas, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }

    [Fact]
    public void Pro_NoTieneWildcardImplicito()
    {
        var plan = new Plan("PRO", "Pro");
        var suscripcion = new Suscripcion(9, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "REPORTES") };

        var permitida = PoliticaModulosSaaS.Evaluar(9, 9, suscripcion, 20, plan, reglas, "REPORTES", Ahora);
        var noDeclarada = PoliticaModulosSaaS.Evaluar(9, 9, suscripcion, 20, plan, reglas, "ADMIN_TOTAL", Ahora);

        Assert.True(permitida.Habilitado);
        Assert.False(noDeclarada.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, noDeclarada.Motivo);
    }

    [Fact]
    public void TenantCruzado_NoPuedeReutilizarDecisionDeOtraEmpresa()
    {
        var plan = new Plan("PRO", "Pro");
        var suscripcionEmpresaB = new Suscripcion(22, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "REPORTES") };

        var decision = PoliticaModulosSaaS.Evaluar(11, 22, suscripcionEmpresaB, 20, plan, reglas, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.TenantNoCoincide, decision.Motivo);
    }

    [Fact]
    public void SinSuscripcionActiva_FallaCerrado()
    {
        var plan = new Plan("FREE", "Free");
        var reglas = new[] { new PlanModulo(10, "INVENTARIO") };

        var sinSuscripcion = PoliticaModulosSaaS.Evaluar(7, 7, null, 10, plan, reglas, "INVENTARIO", Ahora);

        var expirada = new Suscripcion(7, 10, Ahora.AddDays(-10), Ahora.AddDays(-1));
        var conSuscripcionExpirada = PoliticaModulosSaaS.Evaluar(7, 7, expirada, 10, plan, reglas, "INVENTARIO", Ahora);

        Assert.False(sinSuscripcion.Habilitado);
        Assert.False(conSuscripcionExpirada.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SuscripcionNoVigente, sinSuscripcion.Motivo);
        Assert.Equal(MotivoDecisionModuloSaaS.SuscripcionNoVigente, conSuscripcionExpirada.Motivo);
    }

    [Fact]
    public void PlanInactivoONoCoincidente_FallaCerrado()
    {
        var plan = new Plan("PRO", "Pro");
        var suscripcion = new Suscripcion(7, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "REPORTES") };

        plan.Desactivar();
        var inactivo = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 20, plan, reglas, "REPORTES", Ahora);

        plan.Activar();
        var planDistinto = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 21, plan, reglas, "REPORTES", Ahora);

        Assert.False(inactivo.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.PlanNoDisponible, inactivo.Motivo);
        Assert.False(planDistinto.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.PlanNoCoincide, planDistinto.Motivo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ModuloVacioODesconocido_NoHabilita(string? moduloClave)
    {
        var plan = new Plan("FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(10, "INVENTARIO") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 10, plan, reglas, moduloClave, Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.ModuloInvalido, decision.Motivo);
    }

    [Fact]
    public void LimiteCuantitativo_NoSeInterpretaComoFeatureFlag()
    {
        var plan = new Plan("FREE", "Free");
        plan.DefinirLimite("INVENTARIO", 100);
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1));

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 10, plan, Array.Empty<PlanModulo>(), "INVENTARIO", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Single(plan.Limites);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }

    [Fact]
    public void ReglaDesactivada_NoHabilitaModulo()
    {
        var plan = new Plan("PRO", "Pro");
        var suscripcion = new Suscripcion(7, 20, Ahora.AddDays(-1));
        var regla = new PlanModulo(20, "REPORTES");
        regla.Desactivar();

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, 20, plan, new[] { regla }, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }
}
