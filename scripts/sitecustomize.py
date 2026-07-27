import pathlib
import subprocess
import sys
import traceback


def report_exception(exc_type, exc_value, exc_traceback):
    message = ''.join(traceback.format_exception(exc_type, exc_value, exc_traceback))
    pathlib.Path('.github/checkpoints/fase5-transform-error.txt').write_text(message, encoding='utf-8')
    try:
        subprocess.run(['git', 'config', 'user.name', 'VariApp Automation'], check=True)
        subprocess.run(['git', 'config', 'user.email', 'actions@users.noreply.github.com'], check=True)
        subprocess.run(['git', 'add', '.github/checkpoints/fase5-transform-error.txt'], check=True)
        subprocess.run(['git', 'commit', '-m', 'chore(fase5): registrar error de transformación'], check=True)
        subprocess.run(['git', 'push', 'origin', 'HEAD:Desarrollo'], check=True)
    except Exception:
        pass
    sys.__excepthook__(exc_type, exc_value, exc_traceback)


sys.excepthook = report_exception
