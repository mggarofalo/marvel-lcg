from . import *

# Chooser of the Slain

def GetAbilities() -> Sequence['Ability']:

    def chooser_of_the_slain(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            chooser_of_the_slain
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.SearchForCard(Minion, "PutIntoPlayEngagedYou",
            include_encounter_deck=True,
            include_encounter_discard_pile=True)),
    ]

