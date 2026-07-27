from pathlib import Path

path = Path('.github/workflows/catalogos-aceptacion.yml')
text = path.read_text(encoding='utf-8')
text = text.replace('permissions:\n  contents: write\n', 'permissions:\n  contents: read\n', 1)
block = '''  aplicar_fase5:
    name: Aplicar interfaz de imágenes
    runs-on: ubuntu-latest
    steps:
      - name: Checkout Desarrollo
        uses: actions/checkout@v4
        with:
          ref: Desarrollo
          fetch-depth: 0

      - name: Aplicar cambios deterministas
        run: python scripts/fase5_aplicar_interfaz.py

      - name: Restaurar workflow permanente
        run: python scripts/fase5_restaurar_workflow.py

      - name: Retirar utilidades temporales
        run: |
          rm scripts/fase5_aplicar_interfaz.py
          rm scripts/fase5_restaurar_workflow.py
          rm -f .github/workflows/fase5-aplicar-interfaz.yml

      - name: Publicar directamente en Desarrollo
        run: |
          git config user.name "VariApp Automation"
          git config user.email "actions@users.noreply.github.com"
          git add -A
          git commit -m "feat(fase5): integrar imágenes resilientes en módulos operativos"
          git push origin HEAD:Desarrollo

'''
if block not in text:
    raise RuntimeError('No se encontró el job temporal de Fase 5.')
text = text.replace(block, '', 1)
text = text.replace('  acceptance:\n    if: ${{ false }}\n', '  acceptance:\n', 1)
path.write_text(text, encoding='utf-8')
print('Workflow de aceptación restaurado.')
