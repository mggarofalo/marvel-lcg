from engine.config import ConfigVariables
import ast

UNSAFE_LIBS = ConfigVariables.ListStr('unsafe_libs', [
    'asyncio',
    'cgi',
    'ctypes',
    'eval',
    'exec',
    'ftplib',
    'http.client',
    'http',
    'multiprocessing',
    'os',
    'paramiko',
    'pickle',
    'popen2',
    'requests',
    'shlex',
    'shutil',
    'signal',
    'socket',
    'subprocess',
    'subprocess32',
    'tempfile',
    'urllib',
    'webbrowser',
    'win32api',
    'win32com',
    'xmlrpc.client',
    'xmlrpc.server',
    'xmlrpc',
])

class UnsafeCommandVisitor(ast.NodeVisitor):
    def __init__(self) -> None:
        self.unsafe: bool = False

    def visit_Import(self, node: ast.Import) -> None:
        for alias in node.names:
            if alias.name.split('.')[0] in UNSAFE_LIBS.value:
                self.unsafe = True
        self.generic_visit(node)

    def visit_ImportFrom(self, node: ast.ImportFrom) -> None:
        if node.module and node.module.split('.')[0] in UNSAFE_LIBS.value:
            self.unsafe = True
        self.generic_visit(node)

    def visit_Call(self, node: ast.Call) -> None:
        func = node.func
        if isinstance(func, ast.Name):
            if func.id in UNSAFE_LIBS.value:
                self.unsafe = True
        elif isinstance(func, ast.Attribute):
            if func.attr in UNSAFE_LIBS.value:
                self.unsafe = True
        self.generic_visit(node)

def IsCommandSafe(cmd: str) -> bool:
    try:
        tree = ast.parse(cmd)
    except SyntaxError:
        return False

    visitor = UnsafeCommandVisitor()
    visitor.visit(tree)
    return not visitor.unsafe

