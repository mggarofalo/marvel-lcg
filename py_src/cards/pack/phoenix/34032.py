from . import *

# Psychic Assault

def GetAbilities() -> Sequence['Ability']:

    def psychic_assault(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            psychic_assault
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC").SetLabel('attack')
        .SetTarget(Enemy),
    ]

