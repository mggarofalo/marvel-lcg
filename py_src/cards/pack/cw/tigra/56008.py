from . import *

# Tooth and Claw

def GetAbilities() -> Sequence['Ability']:

    def tooth_and_claw(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 4, effect)

        faces = Worlds.GetOnFieldEnemies(effect)
        unit = initiator.AskChooseFace(faces, effect)
        if unit:
            this.DealDamage([unit], 4, effect)


    return [
        AbilityFactory.ReduceCostToPlayThis(
            1,
            each_minion_engaged_with_you=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            tooth_and_claw
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

