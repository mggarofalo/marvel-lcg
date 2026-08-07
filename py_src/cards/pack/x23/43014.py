from . import *

# * Rictor: Julio Richter

def GetAbilities() -> Sequence['Ability']:

    def rictor(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckTopCard(effect)
        value = FacesCounter.GetPrintedResourcesIcon([face], initiator.player_deck)
        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.Response,
            "This",
            rictor
        ).SetTarget("YourEngagedMinions", range="All"),
    ]

