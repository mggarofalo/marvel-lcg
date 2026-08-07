from . import *

# * Pixie: Megan Gwynn

def GetAbilities() -> Sequence['Ability']:

    def pixie(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)

    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            pixie,
        ).SetTarget(Ally, trait="X-MEN", from_where=["YourDiscardPile"]),
    ]

