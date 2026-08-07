from core import *
from aiohttp import web
import ast

from engine.device.web.server.server_base import GameServerBase

class GamePuzzleEditor(GameServerBase):

    async def get_functions(self, request: web.Request) -> web.Response:
        with open('./game/puzzle/puzzle.py', 'r') as file:
            tree = ast.parse(file.read())
            functions = [node.name for node in ast.walk(tree) if isinstance(node, ast.FunctionDef)]
            return web.json_response(functions)

    @override
    def __init__(self) -> None:
        super().__init__()
        # app.router.add_get('/functions', get_functions)
        pass

