from . import *

# * Feral: Maria Callasantos

def GetAbilities() -> Sequence['Ability']:

    def feral(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckTopCard(effect)
        value = FacesCounter.GetPrintedResourcesIcon([face], initiator.player_deck)
        this.DealDamage(effect.targets, value, effect)

    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            feral
        ).SetTarget(Villain),
    ]

