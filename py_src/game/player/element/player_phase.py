from core import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.player import *

class PlayerPhase:

    def __init__(self, player: 'Player') -> None:
        self.player = player

    ################################################################################
    # Phase
    def MayDiscardHandCardsAndDrawUpToMax(self, display_name: str, message: 'Message2'):
        from game.effect.rule import GameRule
        player = self.player
        faces = player.hand_cards.Get()

        value = len(player.GetCountHandSizeFaces()) - player.hand_size
        value = value if value >= 0 else 0

        rule = GameRule(player.GetIdentity(), message, display_name=display_name)
        player.AskDiscardFaces(faces, (value, "All"), rule)
        player.DrawUp("Max", rule)

        # Hand size is enforced *here* and nowhere else, so this is the only
        # place the post-condition holds. `value` above is the minimum the
        # player must discard, so a hand still over the limit after both steps
        # means the discard did not take.
        #
        # This used to be a rule in `game/world/invariants.py`, checked at every
        # decision during `Phase.State.PlaceThreat`. It fired on Thor's printed
        # "Have at thee!" -- draw 2 after a minion engages you -- because any
        # card that draws outside the end phase legitimately puts a hand over
        # its limit until the next one discards it down. There is no decision
        # point during a round where the bound is reliably true, which makes it
        # a post-condition of this operation rather than an invariant of the
        # world. See MARVEL-76 and docs/invariants.md.
        if not player.is_eliminated:
            held = len(player.GetCountHandSizeFaces())
            assert held <= player.hand_size, (
                f"{display_name}: p{player.player_id} holds {held} cards "
                f"against a hand size of {player.hand_size} after discarding "
                f"at least {value} and drawing up")

    def ResolveMulligans(self, message: 'Message.WhenPlayerResolveMulligans') -> None:
        self.MayDiscardHandCardsAndDrawUpToMax("Resolve Mulligans", message)

    def BeginPhase(self) -> None:
        player = self.player
        player.stat.OnBeginPhase()

    def BeginTurn(self) -> None:
        player = self.player
        player.controller.manager.OnBeginTurn()
        player.stat.OnBeginTurn()
        return

    def PlayerTurn(self) -> None:
        from game.message import Message

        player = self.player
        world = player.world

        player.controller.manager.OnPlayerTurnStart()

        turn_begin_message = Message.WhenPlayerTurnBegin(player)
        turn_begin_message.Send()
        begin_message = Message.AfterPlayerTurnBegin(player, turn_begin_message)
        begin_message.Send()

        if player.is_eliminated:
            return

        message = Message.WhenPlayerInTurn(player, world.round_id)
        message.Send()
        if player.is_eliminated:
            return

        turn_end_message = Message.WhenPlayerTurnEnd(player, world.round_id)
        turn_end_message.Send()
        if player.is_eliminated:
            return
        player.res_pool.Reset()
        end_message = Message.AfterPlayerTurnEnd(player, turn_end_message)
        end_message.Send()

    def EndTurn(self):
        player = self.player
        player.stat.OnEndTurn()

    # TODO: use trigger to rewrite?
    def EndPhase(self) -> None:
        from game.card.face.card_face import CardFace
        from game.effect.rule import EndPhase
        from game.operate.faces import Faces

        player = self.player
        message = Message.WhenPlayerEndPhase(player)
        message.Send()

        self.MayDiscardHandCardsAndDrawUpToMax("End Phase", message)

        if player.is_eliminated:
            return

        faces: List[CardFace] = []
        effect = EndPhase(player)
        for face in player.GetControlCards():
            faces.append(face)
            for upgrade in face.GetInventoryDeck().Get():
                faces.append(upgrade)
        Faces.ReadyAll(faces, effect)

        self.player.has_change_form = False
        player.stat.OnEndPhase()

        return

    def BeginRound(self) -> None:
        player = self.player
        player.stat.OnBeginRound()

    def EndRound(self) -> None:
        player = self.player
        player.stat.OnEndRound()

