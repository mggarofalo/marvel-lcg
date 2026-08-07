from . import *

# * Gamora's Sword

def GetAbilities() -> Sequence['Ability']:

    def gamoras_sword(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("ATTACK", Event),
            gamoras_sword
        ).SetTarget(Enemy),
    ]

