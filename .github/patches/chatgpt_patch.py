from pathlib import Path

base_path = Path(__file__).with_name("chatgpt_patch_base.py")
source = base_path.read_text(encoding="utf-8")
old = "var valorAnteriorVariante = variante.Costo * variante.Cantidad;"
new = "var valorAnteriorVariante = (variante.Costo ?? 0m) * variante.Cantidad;"
if source.count(old) != 1:
    raise RuntimeError("No se encontró exactamente una expresión de costo nullable para corregir.")
source = source.replace(old, new, 1)
namespace = {"__file__": str(base_path), "__name__": "__main__"}
exec(compile(source, str(base_path), "exec"), namespace)
