from . import *

# Root Stomp

def GetAbilities() -> Sequence['Ability']:

    def root_stomp(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def action(unit: 'Unit2'):
            initiator = effect.GetInitiator()
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.PlaceCountersOn(targets, 1, 'growth', effect, maximum=10),
                ).SetTarget(name="Groot")
            )

        this.DealDamage(effect.targets, 5, effect, if_this_attack_defeats=action)



    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            root_stomp
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

