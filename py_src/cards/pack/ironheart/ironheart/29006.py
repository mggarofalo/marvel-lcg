from . import *

# Photon Beam

def GetAbilities() -> Sequence['Ability']:

    def photon_beam(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        counter = 1
        def action(unit: 'Unit2'):
            nonlocal counter
            counter = 2

        this.DealDamage(effect.targets, 4, effect, if_this_attack_defeats=action)

        Faces.PlaceCountersOn(effect.targets2, counter, 'progress', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            photon_beam
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2(name="Ironheart"),
    ]

