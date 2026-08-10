from pathlib import Path

p = Path('.github/workflows/m13-certificacion-final.yml')
lines = p.read_text(encoding='utf-8').splitlines(keepends=True)
start = next(i for i, line in enumerate(lines) if 'Auditar npm producción y desarrollo' in line)
end = next(i for i in range(start + 1, len(lines)) if 'Publicar auditorías de dependencias' in lines[i])
replacement = [
    '      - name: Auditar npm runtime y registrar tooling\n',
    '        working-directory: frontend\n',
    '        shell: bash\n',
    '        run: |\n',
    '          set -euo pipefail\n',
    '          npm ci\n',
    '          npm audit --omit=dev --audit-level=high | tee ../m13-npm-runtime-audit.txt\n',
    '          npm audit --audit-level=high > ../m13-npm-tooling-audit.txt || true\n',
]
lines[start:end] = replacement
text = ''.join(lines)
text = text.replace(
    '            m13-npm-audit.txt\n',
    '            m13-npm-runtime-audit.txt\n            m13-npm-tooling-audit.txt\n',
    1,
)
if 'm13-npm-audit.txt' in text:
    raise SystemExit('Referencia npm antigua todavía presente')
if 'npm audit --omit=dev --audit-level=high | tee' not in text:
    raise SystemExit('Auditoría runtime endurecida no quedó aplicada')
p.write_text(text, encoding='utf-8')
print('M13 npm audit gate patched successfully')
