from . import *
from typing import Final

class SenderRound:
    ################################################################################
    # Round and phase
    ################################################################################
    class WhenGameBeginSetup(Message2):
        def __init__(self, campaign: 'CampaignDescriptor', world: 'World') -> None:
            # "07001b", "the_wrecking_crew"
            self.skip_create_encounter_deck = False
            # "11007a", "the_once_and_future_kang"
            self.no_use_obligation: bool = False
            # "24004a"
            self.encounter_set_names: List[str] = campaign.encounter_sets[:]
            self.set_aside_card_ids: List[str] = campaign.set_aside[:]
            # "39025a"
            self.set_aside_modular_card_ids: List[str] = []
            # "21098a"
            self.skip_put_villain_into_play = False
            super().__init__(world=world)

        def CreateSetAside(self):
            from game.card.factory import CardFactory
            from game.message import Message
            CardFactory.GenerateCards(self.set_aside_card_ids, self.world.aside_deck, self.world)
            Message.WhenDeckCreated_Text(self.world.aside_deck)

        def CreateSetAsideModular(self):
            from game.card.factory import CardFactory
            from game.message import Message
            CardFactory.GenerateCards(self.set_aside_modular_card_ids, self.world.aside_modular, self.world)
            Message.WhenDeckCreated_Text(self.world.aside_modular)

    class WhenCampaignSetup(Message2):
        def __init__(self, world: 'World') -> None:
            super().__init__(world=world)

    class WhenGameInitialize(Message2):
        def __init__(self, world: 'World') -> None:
            super().__init__(world=world)

    class WhenGameWouldBegin(Message2):
        def __init__(self, world: 'World') -> None:
            super().__init__(world=world)

    class WhenGameOver(Message2):
        def __init__(self, players_won: bool, reason: str, by_effect: 'Effect') -> None:
            self.players_won = players_won
            self.reason = reason
            self.by_effect = by_effect
            super().__init__(world=by_effect.world)

        def SetPlayerLost(self, by_effect: 'Effect'):
            self.by_effect = by_effect
            self.reason = "Players Lost"
            self.players_won = False

        # End with "GAME_OVER_REASON_RULE"
        def IsRule(self) -> bool:
            # from game.ability.rule import GameRule
            # return isinstance(self.by_effect, GameRule)
            return self.by_effect.is_rule

    class GameOver_Text(TextMessage):
        def __init__(self, players_won: bool, reason: str, by_effect: 'Effect', world: 'World') -> None:
            # from game.ability.rule import GameRule
            super().__init__(world=world)
            assert reason != "Undo"
            # if not isinstance(by_effect, GameRule):
            if not by_effect.is_rule:
                reason = f"{reason} ({by_effect.this.name})"
            text = TransText("\n--- Game Over ---\n{reason}\n{win}", reason=reason, win=players_won)
            self.PresentGameOver(text, "", self.name)

    class WhenRoundStart(Message2):
        def __init__(self, turn_cnt: int, world: 'World') -> None:
            self.turn_cnt: Final = turn_cnt
            # Render.Print(f'\n=== Round {turn_cnt} Start ===')
            super().__init__(world=world)
            text = TransText("\n=== Round {turn_cnt} Start ===", turn_cnt=turn_cnt)
            self.Present(text, "")

    class WhenRoundEnd(HasEndEventMessage):
        def __init__(self, world: 'World') -> None:
            from game.message import Message
            self.round_id: Final = world.round_id
            super().__init__(end_event=Message.AfterRoundEnd, world=world)
            text = TransText("\n--- Round {round_id} End ---", round_id=world.round_id)
            self.Present(text, "nextturn")

    class AfterRoundEnd(HasPreEventMessage):
        def __init__(self, message: 'Message.WhenRoundEnd') -> None:
            self.round_id: Final = message.round_id
            super().__init__(pre_message=message)

    class WhenPhaseBegin(HasEndEventMessage):
        def __init__(self, phase_name: Literal["Player", "Villain"], world: 'World') -> None:
            from game.message import Message
            self.phase_id = world.phase_id
            self.round_id = world.round_id
            self.phase_name = phase_name
            super().__init__(end_event=Message.AfterPhaseBegin, world=world)
            text = TransText("\n--- {phase_name} Phase ---", phase_name=phase_name)
            self.Present(text, "")

    class AfterPhaseBegin(HasPreEventMessage):
        def __init__(self, message: 'Message.WhenPhaseBegin') -> None:
            self.phase_id = message.phase_id
            self.round_id = message.round_id
            self.phase_name = message.phase_name
            super().__init__(pre_message=message)

    class WhenPhaseEnd(HasEndEventMessage):
        def __init__(self, phase_id: int, phase_name: Literal["Player", "Villain"], world: 'World') -> None:
            from game.message import Message
            self.phase_id = phase_id # [1...]
            self.phase_name = phase_name
            super().__init__(end_event=Message.AfterPhaseEnd, world=world)
            text = TransText("\n--- {phase_name} Phase End ---", phase_name=phase_name)
            self.Present(text, "")

    class AfterPhaseEnd(HasPreEventMessage):
        def __init__(self, phase_id: int, phase_name: Literal["Player", "Villain"], message: 'Message.WhenPhaseEnd') -> None:
            self.phase_id = phase_id
            self.phase_name = phase_name
            super().__init__(pre_message=message)

    class WhenVillainPhaseStepStart(HasEndEventMessage):
        def __init__(self, step: int, world: 'World') -> None:
            from game.message import Message
            self.step = step
            super().__init__(end_event=Message.AfterResolveVillainPhaseStep, world=world)

    class AfterResolveVillainPhaseStep(HasPreEventMessage):
        def __init__(self, step: int, message: 'Message.WhenVillainPhaseStepStart') -> None:
            self.step = step
            super().__init__(pre_message=message)

