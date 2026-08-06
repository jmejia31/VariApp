from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / "backend/src/Infrastructure/Services/CargaMasivaService.cs"
text = path.read_text(encoding="utf-8")
old = '''                carga.ErrorGeneral = null;
                confirmadaAhora = true;'''
new = '''                carga.ErrorGeneral = null;
                await _db.SaveChangesAsync(cancellationToken);
                confirmadaAhora = true;'''
if text.count(old) != 1:
    raise RuntimeError(
        f"Se esperaba exactamente una cabecera de carga sin persistencia; encontradas: {text.count(old)}")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Persistencia final de cabecera CargaMasiva agregada.")
