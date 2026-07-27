import builtins
import os
import subprocess
import sys
import traceback


class Path:
    def __init__(self, path):
        self.path = os.fspath(path)

    def read_text(self, encoding='utf-8'):
        with builtins.open(self.path, 'r', encoding=encoding) as file:
            return file.read()

    def write_text(self, content, encoding='utf-8'):
        with builtins.open(self.path, 'w', encoding=encoding) as file:
            return file.write(content)


def report_exception(exc_type, exc_value, exc_traceback):
    message = ''.join(traceback.format_exception(exc_type, exc_value, exc_traceback))
    error_path = '.github/checkpoints/fase5-transform-error.txt'
    with builtins.open(error_path, 'w', encoding='utf-8') as file:
        file.write(message)
    try:
        subprocess.run(['git', 'config', 'user.name', 'VariApp Automation'], check=True)
        subprocess.run(['git', 'config', 'user.email', 'actions@users.noreply.github.com'], check=True)
        subprocess.run(['git', 'add', error_path], check=True)
        subprocess.run(['git', 'commit', '-m', 'chore(fase5): registrar error exacto de transformación'], check=True)
        subprocess.run(['git', 'push', 'origin', 'HEAD:Desarrollo'], check=True)
    except Exception:
        pass
    sys.__excepthook__(exc_type, exc_value, exc_traceback)


sys.excepthook = report_exception

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
