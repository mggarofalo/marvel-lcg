from . import *

# Pulse Grenade

def GetAbilities() -> Sequence['Ability']:

    def pulse_grenade(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(2, effect)
        boost = FacesCounter.CountTotalBoostIcons(faces)

        this.DealDamage(effect.targets, boost, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            pulse_grenade
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Enemy)
    ]

