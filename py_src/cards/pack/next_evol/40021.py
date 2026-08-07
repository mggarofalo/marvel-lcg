from . import *

# * E.V.A.

def GetAbilities() -> Sequence['Ability']:

    def eva_discard(effect: 'Effect') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.DiscardAll([this], effect)

    def eva(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove 1 threat from a scheme",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 1, effect)
            ).SetTarget(Scheme2),
            AbilityFactory.ForChoiceAbility(
                "Deal 1 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetTarget(Enemy),
            AbilityFactory.ForChoiceAbility(
                "Heal 1 damage from Fantomex",
                lambda targets:
                    this.HealthUnits(targets, 1, effect)
            ).SetTarget(name="Fantomex", canbe_heal=True)
        )


    return [
        AbilityFactory.IfCardIsNotInPlay(
            AbilityType.NonKeyword,
            CardFinder(name="Fantomex"),
            eva_discard,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            eva
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

