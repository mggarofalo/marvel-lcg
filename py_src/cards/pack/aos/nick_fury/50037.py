from . import *

# Concentrated Fire

def GetAbilities() -> Sequence['Ability']:

    def concentrated_fire(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        def action(unit: 'Unit2'):
            if Enemy.IsType(unit):
                value = unit.printed_scheme
            else:
                value = 0

            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    f"Place {value} threat on your suit form upgrade",
                    lambda targets:
                        Faces.PlaceTokensOn(targets, value, 'threat', effect)
                ).SetTarget("YourSuitFormUpgrade"),
                ChangeToStealthSuitForm(initiator, effect)
            )


        this.DealDamage(effect.targets, 4, effect, property=AttackProperty(ranged=True), if_this_attack_defeats=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            concentrated_fire
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

