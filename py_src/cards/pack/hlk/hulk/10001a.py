from . import *

# * Hulk

def GetAbilities() -> Sequence['Ability']:

    def hulk(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DiscardAllHands(effect)

    return [
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.ForcedInterrupt,
            "This",
            hulk,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().hand_cards.GetSize() > 0
            ],
        )
    ]

