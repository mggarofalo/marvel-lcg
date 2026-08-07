from . import *

# Stealth Strike

def GetAbilities() -> Sequence['Ability']:

    def stealth_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)


        def action(unit: 'Unit2'):
            initiator = effect.GetInitiator()
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 2, effect)
                ).SetTarget(Scheme2)
            )

        this.DealDamage(effect.targets, 4, effect, if_this_attack_defeats=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            stealth_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

