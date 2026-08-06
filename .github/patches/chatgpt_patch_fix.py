from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / "backend/tests/InventoryApp.Tests/ProductoServiceTests.cs"
text = path.read_text(encoding="utf-8")
old = "        _productoRepoMock.Verify(r => r.Update(producto), Times.Once);"
new = "        _productoRepoMock.Verify(r => r.Update(producto), Times.Never);\n        _productoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);"
if text.count(old) != 1:
    raise RuntimeError("No se encontró exactamente una expectativa antigua de Update(producto).")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Expectativa unitaria de producto alineada con tracking selectivo.")
