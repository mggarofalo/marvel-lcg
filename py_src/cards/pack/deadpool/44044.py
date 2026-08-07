from . import *

# * Negasonic Teenage Warhead: Ellie Phimister

def GetAbilities() -> Sequence['Ability']:

    def negasonic_teenage_warhead(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.CancelWhenRevealedEffect(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.Interrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            negasonic_teenage_warhead
        ).SetCostFunc(CostFunc.DealDamage(2, "This")),
    ]

