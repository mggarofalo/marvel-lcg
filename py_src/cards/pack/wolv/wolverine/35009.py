from . import *

# Slice and Dice

def GetAbilities() -> Sequence['Ability']:

    def slice_and_dice(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(damage: int):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.AttackDamage(targets, damage, effect)
                ).SetLabel('attack').SetTarget(Enemy)
            )

        action(3)
        action(3)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            slice_and_dice
        ).SetPlay()
        .SetTarget2(Enemy)
    ]

