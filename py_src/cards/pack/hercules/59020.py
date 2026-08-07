from . import *

# * Thor: Odinson

def GetAbilities() -> Sequence['Ability']:

    def thor(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if minion:
            minion.PutIntoPlay(initiator, effect)


    return [
        AbilityFactory.AfterUnitMakeThwart(
            AbilityType.ForcedResponse,
            "This",
            thor,
            is_basic_thwart=True
        ),
    ]

