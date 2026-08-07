from . import *

# Grande Finale

def GetAbilities() -> Sequence['Ability']:

    def grande_finale(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 2, effect)
        value = effect.GetPaidResources().GetResourceIconTypes()
        for _ in range(value):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        this.DealDamage(targets, 2, effect)
                ).SetLabel('attack').SetTarget(Enemy)
            )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            grande_finale
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

