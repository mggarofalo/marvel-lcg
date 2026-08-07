from engine.network import WebServer

class Editor:

    server: WebServer
    ip = "localhost"
    port = 2340

    @staticmethod
    def Initialize() -> None:
        from editor.editor_http import EditorHttp
        Editor.server = EditorHttp()

    @staticmethod
    def EditorRun() -> None:
        Editor.server.Run(Editor.ip, Editor.port, "Editor")

    @staticmethod
    def Shutdown() -> None:
        Editor.server.Shutdown()

