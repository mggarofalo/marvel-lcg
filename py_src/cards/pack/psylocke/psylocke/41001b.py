from . import *

# * Betsy Braddock

def GetAbilities() -> Sequence['Ability']:

    def betsy_braddock(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

    return [
        AbilityFactory.SetupPutIntoPlay(
            [
                "41002a,41002b",
                "41002a,41002b"
            ],
            flip_to_name="Psi-Knife",
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            betsy_braddock
        ).SetCostFunc(CostFunc.Exhaust(
            trait="PSI-ENERGY",
            card_type=Upgrade,
            from_where=["YouControlCards"])
        ).SetTarget(trait="PSIONIC", from_where=["YourDiscardPile"]),
    ]

