from . import *

# Vibranium Suit

def GetAbilities() -> Sequence['Ability']:

    def vibranium_suit(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.MoveDamage(1, effect.targets[0].CastTo(Unit2), effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard this card to give your hero a tough status card",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetCostFunc(CostFunc.Discard("This"))
            .SetTarget("YourHero")
        )


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            vibranium_suit
        ).SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2("YourHero", sustained=True)
    ]

