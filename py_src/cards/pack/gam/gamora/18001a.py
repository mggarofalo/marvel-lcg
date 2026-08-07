from . import *

# * Gamora

def GetAbilities() -> Sequence['Ability']:

    def gamora(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)

    def gamora2(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("ATTACK", Event),
            gamora
        ).SetTarget(Scheme2)
        .SetName("Finesse")
        .LimitOncePerPhase(),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("THWART", Event),
            gamora2
        ).SetTarget(Enemy)
        .SetName("Precision")
        .LimitOncePerPhase(),
    ]

