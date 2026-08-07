from . import *

# Air Supremacy

def GetAbilities() -> Sequence['Ability']:

    def air_supremacy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            air_supremacy
        ).SetPlay()
        .SetTarget(Enemy,
            range=(1,
                lambda effect:
                    len([x for x in effect.GetInitiator().GetControlCharacters() if x.HasTrait('AERIAL')])))
    ]

