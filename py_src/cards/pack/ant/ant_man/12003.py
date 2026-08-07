from . import *

# Giant Stomp

def GetAbilities() -> Sequence['Ability']:

    def giant_stomp(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)
        this.DealDamage(effect.targets2, 8, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            giant_stomp,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "GIANT")
            ],
        ).SetPlay().SetLabel('attack')
        .SetTarget(Minion, range="All")
        .SetTarget2(Enemy)
    ]

