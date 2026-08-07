from . import *

# Espionage

def GetAbilities() -> Sequence['Ability']:

    def espionage(effect: 'Effect', message: 'Message.WhenSurgeWouldBeResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_you_control_trait="SPY"),
        AbilityFactory.WhenSurgeWouldBeResolved(
            AbilityType.Interrupt,
            EncounterCard,
            espionage,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

