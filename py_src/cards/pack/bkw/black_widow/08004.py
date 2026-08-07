from . import *

# Dance of Death

def GetAbilities() -> Sequence['Ability']:

    def dance_of_death(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
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

        action(1)
        action(2)
        action(3)

    return [
        # The card has the Attack trait and the ability says
        # “Make the following 3 attacks in order” so it is clearly an attack.
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            dance_of_death
        ).SetPlay().SetLabel()
        .SetTarget2(Enemy)
    ]

