from core import *

class File:
    def __init__(self, file_name: str, *, read: bool=False, write: bool=False, bin: bool=False):
        mode = ""
        encoding = None
        if read:
            if bin:
                mode = "rb"
            else:
                mode = "r"
                encoding="utf-8"
        if write:
            if bin:
                mode = "wb"
            else:
                mode = "w"
                encoding="utf-8"
        assert mode != ""
        self.file = open(file_name, mode, encoding=encoding)
        self.file_name = file_name

    def __enter__(self):
        return self

    def __exit__(self, *obj: object):
        self.Close()

    def Close(self):
        self.file.close()

    def Read(self):
        return self.file.read()

    def Write(self, content: Any):
        return self.file.write(content)

    def GetMimeType(self) -> str:
        from engine.lib import MimeType
        return MimeType.GetMimeType(self.file_name)
