from . import *

# * Stepford Cuckoos

def GetAbilities() -> Sequence['Ability']:

    def stepford_cuckoos(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)

        player = message.GetToPlayer()
        Worlds.RevealEncounterDeckTopCard(player, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "AnyPlayer",
            Treachery,
            "All",
            stepford_cuckoos,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'psi'))
    ]
