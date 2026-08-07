from . import *

# * Leo Fitz

def GetAbilities() -> Sequence['Ability']:

    def leo_fitz(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="TECH"
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.ReduceCostToPlayThis(
            lambda effect: 2,
            your_identity_has_traits=["S.H.I.E.L.D"]
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            leo_fitz
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

