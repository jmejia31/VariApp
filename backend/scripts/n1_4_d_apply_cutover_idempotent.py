from pathlib import Path
import runpy

service_path = Path("backend/src/Application/Services/AjusteInventarioService.cs")
dto_path = Path("backend/src/Application/DTOs/AjusteStockDto.cs")
service = service_path.read_text(encoding="utf-8-sig")

# Idempotencia para ejecuciones posteriores al commit del cutover.
if "private AjusteInventarioExistenciaCutoverService CrearCutoverExistencias()" in service:
    print("N1.4.D cutover ya materializado; no hay transformación pendiente.")
    raise SystemExit(0)

# El transformador certificado original espera el DTO legacy. Si el contexto físico
# ya fue publicado de forma independiente, normalizarlo temporalmente y dejar que
# el transformador vuelva a materializar exactamente la salida certificada.
dto = dto_path.read_text(encoding="utf-8-sig")
dto = dto.replace("    public int AlmacenId { get; set; }\n", "")
dto = dto.replace("    public int? UbicacionAlmacenId { get; set; }\n", "")
dto_path.write_text(dto, encoding="utf-8")

runpy.run_path("backend/scripts/n1_4_d_transform_ajuste_writer.py", run_name="__main__")
