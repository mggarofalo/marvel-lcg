from . import *

# Pivotal Moment

def GetAbilities() -> Sequence['Ability']:

    def pivotal_moment(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 2
        scheme = Worlds.FindMainScheme(effect)
        if not scheme or scheme.threat == 0:
            damage = 5
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            pivotal_moment
        ).SetPlay().SetLabel('attack')
        .SetTarget(Villain),
    ]

