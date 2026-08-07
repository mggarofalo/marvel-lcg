from . import *

# One by One

def GetAbilities() -> Sequence['Ability']:

    def one_by_one(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def action(unit: 'Unit2'):
            initiator = effect.GetInitiator()
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetLabel('attack').SetTarget(Enemy)
            )

        this.DealDamage(effect.targets, 2, effect, if_this_attack_defeats=action)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            one_by_one
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

