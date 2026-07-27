import builtins
import os


class Path:
    def __init__(self, path):
        self.path = os.fspath(path)

    def read_text(self, encoding='utf-8'):
        with builtins.open(self.path, 'r', encoding=encoding) as file:
            return file.read()

    def write_text(self, content, encoding='utf-8'):
        with builtins.open(self.path, 'w', encoding=encoding) as file:
            return file.write(content)


workflow = '.github/workflows/catalogos-aceptacion.yml'
if os.path.exists(workflow):
    with builtins.open(workflow, 'r', encoding='utf-8') as file:
        text = file.read()
    if '# FASE5_MARKER_START' not in text:
        text += '''
# FASE5_MARKER_START
            e2e/fase4-responsive.spec.ts \
            e2e/phase7-admin-role.spec.ts
# FASE5_MARKER_END
'''
        with builtins.open(workflow, 'w', encoding='utf-8') as file:
            file.write(text)
