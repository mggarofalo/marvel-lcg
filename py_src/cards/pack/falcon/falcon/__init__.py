from cards.pack import *

class Buff_53001a(Buff):
    def __init__(self) -> None:
        super().__init__()
        self.face: 'CardFace|None' = None

    def Update(self, face: 'CardFace|None'):
        self.face = face

