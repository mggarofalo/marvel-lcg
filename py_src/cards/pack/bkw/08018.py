from . import *

# Spycraft

def GetAbilities() -> Sequence['Ability']:

    def spycraft(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)

        initiator = effect.GetInitiator()
        Worlds.RevealEncounterDeckTopCard(initiator, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_you_control_trait="SPY"),
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "You",
            EncounterCard,
            "All",
            spycraft
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

