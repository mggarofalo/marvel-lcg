from . import *

# Shapeshifter Strike

def GetAbilities() -> Sequence['Ability']:

    def shapeshifter_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)
        Faces.ReadyAll(effect.targets2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            shapeshifter_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2("YouControlCards", trait="SHAPESHIFT", canbe_ready=True),
    ]

