from core import *
from game.card.face import *
from game.card.face.base import *
from game.ability import *
from game.message import *
from game.player import *
from game.deck import *
from cards.paper import Paper
from game.element.damage_property import DamageProperty

class Villain(Enemy, HasStage, HasAccelerationIcon, HasHazard, HasAmplify, HasVictory, EncounterCard):

    VILLAIN_SCHEME: Dict[str, str] = {
    }

    @override
    def __init__(self, paper: 'Paper') -> None:
        # self.stage = 0
        self.encounter_deck: 'Deck'
        self.encounter_discard_pile: 'Deck'
        super().__init__(paper)

    # @override
    # def SetName(self, paper: 'Paper'):
    #     name = paper.name
    #     stage_dict: Dict[str, int] = {
    #         " (I)": 1,
    #         " (II)": 2,
    #         " (III)": 3,
    #     }
    #     for stage in stage_dict:
    #         if name.endswith(stage):
    #             self.stage = stage_dict[stage]
    #             name = name.removesuffix(stage)
    #             break
    #     self.name = name

    @override
    def OnWhenCardRevealed(self, revealed_message: 'Message.WhenCardRevealed') -> None:
        if not self.IsInPlay():
            self.PutIntoPlay(revealed_message.GetToPlayer(), revealed_message.by_effect)
        return super().OnWhenCardRevealed(revealed_message)

    @override
    def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> 'bool':
        from game.operate.faces import Faces
        # assert self.card.area.IsVillainArea(), f"{self.card.area=}"
        # You need to call `villain.card.MoveToArea(world.area_villain)` first
        if not self.card.area.flags.is_villain_area:
            scenario = self.GetControlByOrOwner()
            if Scenario.IsType(scenario):
                self.SetEncounterDeck(scenario.encounter_deck)
                area_villain = scenario.area_villain
                Faces.MoveAllTo([self], area_villain, message.by_effect)
        return super().OnPutIntoPlay(message)

    @override
    def OnBeDefeated(self, would_defeated_message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated', *, as_asset: bool, ignore_when_defeated: bool):
        super().OnBeDefeated(would_defeated_message, as_asset=as_asset, ignore_when_defeated=ignore_when_defeated)
        if self.IsInPlay() or self.card.area.flags.is_victory_display:
            self.Advance(would_defeated_message.by_effect)

    @staticmethod
    def GetGuardFaces(effect: 'Effect') -> Sequence['CardFace']:
        if effect.initiator.IsPlayer():
            return [minion for minion in effect.GetInitiator().GetEngagedMinions() if minion.IsGuard()]
        else:
            return []

    @override
    def CanBeAttackBy(self, effect: 'Effect') -> bool:
        if not effect.IsIgnoreKeyword('Guard', effect) and type(effect.initiator) is Player:
            for minion in effect.GetInitiator().GetEngagedMinions():
                if minion.IsGuard():
                    return False
        return super().CanBeAttackBy(effect)

    @override
    def GetBoostCardNum(self, message: 'Message2') -> int:
        return 1

    @override
    def TakeDamageWithOverkillTarget(self, source: 'CardFace', property: 'int|DamageProperty',
                                    by_effect: 'Effect',
                                    would_attack_unit_message: 'Message.WhenUnitWouldAttackUnit|None'=None,
                                    overkill_target: 'Unit2|None'=None,
                                    *,
                                    from_atk_message: 'Message.WhenUnitWouldAttack|None'=None, # Fix for indirect damage, "43036"
                                    operation: Callable[['Message.AfterUnitTookDamage'], None]|None=None
                                    ) -> List['Message.AfterUnitDefeatedUnit|Message.AfterUnitTookDamage']:
        ignore_guard = False
        guard_faces: Sequence[CardFace] = []
        if by_effect.IsIgnoreKeyword('Guard', by_effect):
            guard_faces = Villain.GetGuardFaces(by_effect)
            if guard_faces:
                ignore_guard = True

        ret_value = super().TakeDamageWithOverkillTarget(source, property, by_effect, would_attack_unit_message, from_atk_message=from_atk_message, operation=operation)

        if ignore_guard:
            ignore_message = Message.AfterIgnoreKeywordOnCard(source, guard_faces, "Guard")
            ignore_message.Send()

        return ret_value

    ################################################################################
    #
    def Advance(self, by_effect: 'Effect', to_villain: 'Villain|None'=None, move_old_villain_to: 'Deck|None'=None) -> None:
        scenario = self.GetControlByOrOwner()
        if Scenario.IsType(scenario):
            scenario.AdvanceVillainStage(self, by_effect, to_villain, move_old_villain_to)

    ################################################################################
    #
    def SetActive(self, by_effect: 'Effect'):
        from game.operate.faces import Faces
        from game.operate.worlds import Worlds

        for other_villain in Worlds.GetVillains(by_effect):
            if other_villain != self and other_villain.IsActive():
                Faces.RemoveCountersOn([other_villain], 1, 'active', by_effect)
        if not self.IsActive():
            Faces.PlaceCountersOn([self], 1, 'active', by_effect)

    def IsActive(self) -> bool:
        return self.GetCounters('active') > 0

    ################################################################################
    #
    def FindCorrespondingScheme(self) -> 'Scheme2|None':
        from game.operate.worlds import Worlds
        scheme = None
        if self.name in Villain.VILLAIN_SCHEME:
            name = Villain.VILLAIN_SCHEME[self.name]
            faces = Worlds.GetOnFieldCards(self.card.game_area)
            schemes = CardFinder(name=name).Checks2(faces, Scheme2)
            if schemes:
                scheme = schemes[0]
        return scheme

    def SetEncounterDeck(self, deck: 'Deck'):
        self.encounter_deck = deck
        assert deck.bind_discard_pile
        self.encounter_discard_pile = deck.bind_discard_pile

        if self == self.card.face:
            for face in self.card.back_faces:
                if Villain.IsType(face):
                    face.SetEncounterDeck(deck)

    ################################################################################
    #
    def ChangeToFace(self, card_face: 'Villain', by_effect: 'Effect') -> bool:
        return self.DefaultChangeToFace(card_face, by_effect)

    def ChangeToForm(self, to_trait: 'CardFace.TRAITS', by_effect: 'Effect'):
        finder = CardFinder2(to_trait)
        return self.DefaultChangeToForm(finder, by_effect)

    def IsFinalStage(self, effect: 'Effect') -> bool:
        if effect.world.GetScenario().villain_deck.FindCard(name=self.name):
            return False
        return True

