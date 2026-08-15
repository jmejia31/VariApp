from pathlib import Path

path = Path("backend/src/Application/Services/AjusteInventarioService.cs")
source = path.read_text(encoding="utf-8-sig")
class_marker = "public sealed class AjusteInventarioService : IAjusteInventarioService\n{\n"
if source.count(class_marker) != 1:
    raise SystemExit("No se encontró exactamente una declaración canónica de AjusteInventarioService.")

prefix, body = source.split(class_marker, 1)
if not body.endswith("}\n"):
    raise SystemExit("El servicio no termina con el cierre esperado.")
body = body[:-2]

markers = [
    "    public async Task<AjusteInventarioDto?> AnularAsync",
    "    private async Task<AjusteInventario> CrearBorradorInternoAsync",
    "    private static void ValidarCabecera("
]
positions = []
for marker in markers:
    if body.count(marker) != 1:
        raise SystemExit(f"Marker ambiguo o ausente: {marker}")
    positions.append(body.index(marker))

parts = [
    body[:positions[0]],
    body[positions[0]:positions[1]],
    body[positions[1]:positions[2]],
    body[positions[2]:]
]
outputs = [
    ("AjusteInventarioService.N14.Core.cs", True),
    ("AjusteInventarioService.N14.Anular.cs", False),
    ("AjusteInventarioService.N14.Internal.cs", False),
    ("AjusteInventarioService.N14.Helpers.cs", False)
]

for (name, implements_interface), fragment in zip(outputs, parts):
    declaration = (
        "public sealed partial class AjusteInventarioService : IAjusteInventarioService"
        if implements_interface
        else "public sealed partial class AjusteInventarioService"
    )
    target = path.parent / name
    target.write_text(prefix + declaration + "\n{\n" + fragment + "}\n", encoding="utf-8")
    print(f"{target}: {target.stat().st_size} bytes")

path.unlink()
print("Servicio monolítico retirado del workspace efímero; reemplazo partial listo.")
