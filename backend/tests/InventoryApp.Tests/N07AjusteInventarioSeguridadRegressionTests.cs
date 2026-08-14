using Xunit;

namespace InventoryApp.Tests;

public sealed class N07AjusteInventarioSeguridadRegressionTests
{
    [Fact]
    public void Ajustes_stock_legacy_deben_exigir_confirmar_inventario()
    {
        var source = Leer("backend/src/API/Controllers/InventarioAjustesController.cs");

        Assert.DoesNotContain(
            "RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)",
            source);

        const string permisoEsperado =
            "RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Confirmar)";

        Assert.Equal(2, ContarOcurrencias(source, permisoEsperado));
    }

    [Fact]
    public void Ajustes_stock_legacy_deben_permanecer_autenticados()
    {
        var source = Leer("backend/src/API/Controllers/InventarioAjustesController.cs");

        Assert.Contains("[Authorize]", source);
        Assert.Contains("[HttpPost(\"{productoId:int}/ajustes-stock\")]", source);
        Assert.Contains(
            "[HttpPost(\"{productoId:int}/variantes/{varianteId:int}/ajustes-stock\")]",
            source);
    }

    private static int ContarOcurrencias(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Leer(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "src")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        var path = Path.Combine(
            directory!.FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"No se encontró el archivo de fuente requerido: {path}");
        return File.ReadAllText(path);
    }
}
