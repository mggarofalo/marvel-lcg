from aiohttp import web
import io
from PIL import Image
from urllib.parse import parse_qs

from engine.lib import Json
from engine.log import Log
from engine.network import WebServer
from engine.file import Cache
from engine.config import ConfigVariables

CATEGORY_NAME = "EDITOR"

CARDS_JSON_FILE = ConfigVariables.File('cards_json_file')

class EditorHttp(WebServer):
    def __init__(self) -> None:
        super().__init__()

        self.AddDefaultGet()

        self.AddHtmlSecurity('/', './public/editor.html')

        self.AddAwaitGetSecurity('/get_cards_json', self.handle_get_cards_json)

        self.AddAwaitGetSecurity('/{filename:.*}', self.handle_image)
        self.AddPostSecurity('/process', self.handle_post)

        self.cards_json_file = CARDS_JSON_FILE.value

    async def handle_get_cards_json(self, request: web.Request):
        data = Json.Load(self.cards_json_file)
        return web.json_response(data)

    async def handle_image(self, request: web.Request):
        file_path = request.path
        image_bytes = Cache.LoadImage(file_path)

        def image_to_byte_array(image: Image.Image) -> bytes:
            bytes_io = io.BytesIO()
            image.save(bytes_io, format='jpeg')
            return bytes_io.getvalue()

        img_io = Image.open(io.BytesIO(image_bytes))
        width, height = img_io.size
        if width > height:
            img_io = img_io.rotate(90, expand=True)
            image_bytes = image_to_byte_array(img_io)

        return web.Response(body=image_bytes, content_type='image/jpeg')

    async def handle_post(self, request: web.Request):
        body = await request.read()
        data = parse_qs(body.decode())
        Log.Debug(CATEGORY_NAME, data)
        from editor.editor_create import DoEditor
        DoEditor(data)
        return web.Response(text=str(data))

