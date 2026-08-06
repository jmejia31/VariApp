from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / "backend/tests/InventoryApp.Tests/InventoryConcurrencyTests.cs"
text = path.read_text(encoding="utf-8")
old = 'x.ReferenciaTipo == "Venta" && x.ReferenciaId.HasValue && ventaIds.Contains(x.ReferenciaId.Value)'
new = 'x.ReferenciaTipo == "Venta" && ventaIds.Contains(x.ReferenciaId)'
if text.count(old) != 1:
    raise RuntimeError(
        "Se esperaba exactamente una consulta de inventario con ReferenciaId tratado como nullable.")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Consulta concurrente de movimientos de inventario corregida.")
