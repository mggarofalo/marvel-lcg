from . import *

# * Remy Lebeau

def GetAbilities() -> Sequence['Ability']:

    def remy_lebeau(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        faces = effect.cost_func.Get(CostFunc.LookAtDeck).return_discard_cards
        value = FacesCounter.CountTotalBoostIcons(faces)
        this.RemoveThreatFromSchemes(effect.targets2, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            remy_lebeau
        ).SetName("Thief Extraordinaire")
        .SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.LookAtDeck("EncounterDeck", 2, discard=1))
        .SetTarget2(Scheme2),
    ]

