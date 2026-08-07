from . import *

# Shoulder Cannon

def GetAbilities() -> Sequence['Ability']:

    def shoulder_cannon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 1, effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove 1 ammo from War Machine to ready shoulder cannon",
                lambda targets:
                    Faces.ReadyAll(targets, effect)
            ).SetCostFunc(CostFunc.Counter(CardFinder(name="War Machine"), 1, 'ammo'))
            .SetTarget([this])
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            shoulder_cannon
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy)
    ]

