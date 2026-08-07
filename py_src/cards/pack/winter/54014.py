from . import *

# Firepower

def GetAbilities() -> Sequence['Ability']:

    def firepower(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(targets: Sequence['CardFace']):
            this.DealDamage(targets, 3, effect, property=AttackProperty(ranged=True))

        action(effect.targets)

        size = len(effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards)
        for _ in range(size - 1):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    action
                ).SetLabel('attack').SetTarget(Enemy)
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            firepower
        ).SetPlay().SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("YouControlCards", trait="WEAPON", card_type=Upgrade, range=(1, 3)))
        .SetTarget(Enemy),
    ]

