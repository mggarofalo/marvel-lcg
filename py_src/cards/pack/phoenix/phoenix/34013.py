from . import *

# Phoenix Firebird

def GetAbilities() -> Sequence['Ability']:

    def phoenix_firebird(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove 1 power counter from Phoenix Force → ready Phoenix",
                lambda targets:
                    Faces.ReadyAll(targets, effect)
            ).SetTarget(name="Phoenix", canbe_ready=True)
            .SetCostFunc(CostFunc.Counter(CardFinder(name="Phoenix Force"), 1, 'power')),
            AbilityFactory.ForChoiceAbility(
                "Place 2 power counters on Phoenix Force",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 2, 'power', effect)
            ).SetTarget(name="Phoenix Force"),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            phoenix_firebird
        ).SetPlay().SetLabel()
        .SetTarget2(name="Phoenix", canbe_ready=True)
        .SetTarget2(name="Phoenix Force"),
    ]

