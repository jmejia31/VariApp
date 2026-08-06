from pathlib import Path

base_path = Path(__file__).with_name("chatgpt_patch_base.py")
source = base_path.read_text(encoding="utf-8")
old_nullable = "var valorAnteriorVariante = variante.Costo * variante.Cantidad;"
new_nullable = "var valorAnteriorVariante = (variante.Costo ?? 0m) * variante.Cantidad;"
if source.count(old_nullable) != 1:
    raise RuntimeError("No se encontró exactamente una expresión de costo nullable para corregir.")
source = source.replace(old_nullable, new_nullable, 1)
namespace = {"__file__": str(base_path), "__name__": "__main__"}
exec(compile(source, str(base_path), "exec"), namespace)

root = Path(__file__).resolve().parents[2]
for relative_path, document_name in (
    ("backend/tests/InventoryApp.Tests/VentaServiceTests.cs", "venta"),
    ("backend/tests/InventoryApp.Tests/CompraServiceTests.cs", "compra"),
):
    path = root / relative_path
    text = path.read_text(encoding="utf-8")
    old = ".ReturnsAsync(new InventarioLockSet(productos, variantes));"
    new = f'''.ReturnsAsync(new InventarioLockSet(
                productos,
                variantes,
                {document_name}.Detalles
                    .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                    .ToList()));'''
    if text.count(old) != 1:
        raise RuntimeError(f"{relative_path}: se esperaba una fábrica de InventarioLockSet y no se encontró exactamente una.")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")

print("Mocks de inventario actualizados con demandas consolidadas.")
