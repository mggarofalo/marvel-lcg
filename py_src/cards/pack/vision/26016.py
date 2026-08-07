from . import *

# Flow Like Water

def GetAbilities() -> Sequence['Ability']:

    def flow_like_water(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players",
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("DEFENSE"),
            flow_like_water,
        ).SetTarget("Attacker")
    ]

