from cards.pack import *

class Buff_49001a(Buff):
    def __init__(self) -> None:
        super().__init__()
        self.faces: List['CardFace'] = []

    def Update(self, faces: List['CardFace']):
        self.faces = faces

