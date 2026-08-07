from . import *

class HasSetup(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_setup = 0

        super().__init__(paper)

        self.RegisterAttribute("Setup", "printed_setup")

    def ProcessSetup(self, by_effect: 'Effect'):
        from game.operate.worlds import Worlds
        if self.printed_setup:
            player = self.GetControlByOrOwner()
            if not Player.IsType(player):
                player = Worlds.GetFirstPlayer(by_effect)
            self.PutIntoPlay(player, by_effect)

