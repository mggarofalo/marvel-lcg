from . import *

# 'Port and Punch

def GetAbilities() -> Sequence['Ability']:

    def port_and_punch(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.DealDamage(targets, 3, effect)
            ).SetLabel('attack')
            .SetTarget(Enemy, with_attach=CardFinder(name="Bamf!"), range="All")
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            port_and_punch
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

