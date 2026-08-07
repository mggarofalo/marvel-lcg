from . import *

class HasAlliance(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.alliance = False

        super().__init__(paper)

        self.RegisterAttribute("Alliance", "alliance", bool)

