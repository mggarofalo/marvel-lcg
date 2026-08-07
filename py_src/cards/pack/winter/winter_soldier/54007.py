from . import *

# * Safe House #30

def GetAbilities() -> Sequence['Ability']:

    def safe_house_30(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.EncounterCard(
            effect,
            initiator,
            include_discard_pile=False,
            card_type=Minion,
        )
        if face:
            face.PutIntoPlay(initiator, effect)
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            safe_house_30
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

