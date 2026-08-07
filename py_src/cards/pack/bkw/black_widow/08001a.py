from . import *

# * Black Widow

def GetAbilities() -> Sequence['Ability']:

    def black_widow(effect: 'Effect', message: 'Message.WhenEffectWouldResolve|Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        # AbilityFactory.AfterPlayerTriggerAbility(
        #     AbilityType.Response,
        #     "You",
        #     CardFinder2("PREPARATION"),
        #     black_widow,
        #     card_control_by="You",
        # ).SetTarget(Enemy)
        # .SetName("Widowmaker"),
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            CardFinder2("PREPARATION"),
            black_widow,
            control_by_you=True
        ).SetTarget(Enemy)
        .SetName("Widowmaker"),
    ]

