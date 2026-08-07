from . import *

# Shadowcat Surprise

def GetAbilities() -> Sequence['Ability']:

    def shadowcat_surprise(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)
        Faces.ReadyAll(effect.targets2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            shadowcat_surprise
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2("YourHero", canbe_ready=True),
    ]

