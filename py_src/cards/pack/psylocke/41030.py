from . import *

# Psi-Bow Attack

def GetAbilities() -> Sequence['Ability']:

    def psi_bow_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect, property=AttackProperty(ranged=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            psi_bow_attack
        ).SetPlay(only_if_your_hero_has_trait="PSIONIC").SetLabel('attack')
        .SetTarget(Enemy),
    ]

