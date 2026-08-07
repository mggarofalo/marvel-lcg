from . import *

# * Peni Parker

def GetAbilities() -> Sequence['Ability']:

    def peni_parker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)


    return [
        AbilityFactory.SetupPutIntoPlay(
            ["31001a,31001b"],
            flip_to_trait="INACTIVE"
        ).SetName("Psychogenetic Compatibility"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            peni_parker
        ).SetName("Maintenance")
        .SetCostFunc(CostFunc.Exhaust(name="SP//dr Suit")),
    ]

