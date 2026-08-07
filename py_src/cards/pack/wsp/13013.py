from . import *

# Into the Fray

def GetAbilities() -> Sequence['Ability']:

    def into_the_fray(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(value: int):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, value, effect),
                ).SetTarget(MainScheme),
            )

        this.DealDamage(effect.targets, 6, effect, if_this_attack_deal_excess_damage=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            into_the_fray
        ).SetPlay().SetLabel('attack')
        .SetTarget(Minion),
    ]

