from . import *

# Scout Ahead

def GetAbilities() -> Sequence['Ability']:

    def scout_ahead(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 3, effect)
            ).SetTarget(Scheme2, exclude=effect.targets)
            .SetCostFunc(CostFunc.Discard(
                "YourHandCards",
                name="Bamf!"))
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            scout_ahead
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

