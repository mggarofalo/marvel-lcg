from . import *

# Molecular Decay

def GetAbilities() -> Sequence['Ability']:

    def molecular_decay(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(2, effect)
        damage = 5
        damage += FacesCounter.CountTotalBoostIcons(faces)

        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            molecular_decay
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

