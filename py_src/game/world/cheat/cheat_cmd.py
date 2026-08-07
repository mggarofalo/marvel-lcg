from core import *
from game.card.face.card_face import CardFace
from game.world import *

CATEGORY_NAME = "CHEAT"

################################################################################
#
class CheatCommand:

    def __init__(self, world: 'World') -> None:
        self.world = world
        self.test_cards: Set[str] = set()

    def LogDebugCommand(self, face: 'CardFace'):
        card_id = face.paper.card_id
        self.test_cards.add(card_id)
        # Log.Notify(CATEGORY_NAME, "", card_id)
        # if not Test.IsInTesting():
        #     Log.Debug(CATEGORY_NAME, card_id)

    def DebugBreak(self):
        return self.DebugExec(['break'])

    def DebugExec(self, commands: Sequence[str]):

        player_id = 0

        if len(commands) == 1 and commands[0].startswith('eliminate'):
            command = commands[0]
            player_id = int(command[len('eliminate'):])
            player = self.world.const_seat_order_players[player_id]
            if not player.is_eliminated:
                commands = [f"Eliminate(player_id)"]
            else:
                return

        from game.world.cheat.cheat_cmd_helper import RunCheat
        RunCheat(self.world, commands, player_id)

