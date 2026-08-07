from . import *

# Operation Zero Tolerance

def GetAbilities() -> Sequence['Ability']:

    def operation_zero_tolerance(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        ally = message.target
        this.PlaceCardHere([ally], False, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            Enemy,
            Ally,
            operation_zero_tolerance
        ),
        AbilityFactory.AfterCardsMoved(
            AbilityType.NonKeywordBold,
            None,
            lambda effect, message:
                Worlds.SetGameOver(False, effect),
            conditions=[
                lambda effect, message:
                    effect.this.GetPlacedCardArea().FindCardSize(CardFinder(is_face_up=False)) >= Worlds.GetPlayerNum(effect) + 3
            ]
        ),
    ]

