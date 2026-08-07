from . import *

# * Maria Hill

def GetAbilities() -> Sequence['Ability']:

    def maria_hill(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="S.H.I.E.L.D",
            card_type=Support
        )
        if face:
            Faces.AddToHand([face], initiator, effect)

    return [
        # You may include the maximum number of copies of 3 [[S.H.I.E.L.D.]] supports in your deck from aspects other than your chosen aspect.
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            maria_hill
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

