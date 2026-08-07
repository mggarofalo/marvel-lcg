from . import *

class HasMaxPer(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.max_per_unit = 256 # TODO: Rename max_per_card

        super().__init__(paper)

        self.RegisterAttribute("MaxPerUnit", "max_per_unit")

