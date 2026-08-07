from . import *

# One Way or Another

def GetAbilities() -> Sequence['Ability']:

    def one_way_or_another(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            one_way_or_another
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.SearchForCard(SchemeSide2, "Reveal", include_encounter_deck=True))
        .MaxOnePerRound(),
    ]

