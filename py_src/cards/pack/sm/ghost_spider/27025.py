from . import *

# Worried Father

def GetAbilities() -> Sequence['Ability']:

    def worried_father(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        face = Search.PlayerCard(
            effect,
            player,
            include_player_deck=True,
            include_player_hand_cards=True,
            include_discard_pile=True,
            include_player_area=True,
            name="George Stacy",
            card_type=Support
        )

        if face:
            # Attaches George Stacy to itself instead of setting 
            # him aside and attaching to him.
            this.PlaceCardHere(face, "Peek", effect)

    def worried_father_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        initiator = effect.GetInitiator()
        face = Find.Find(
            effect,
            who_perform=initiator,
            name="George Stacy",
            card_type=Support,
        )
        if face:
            Faces.AddToHand([face], initiator, effect)

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            worried_father
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            worried_father_action
        ).SetCostFunc(CostFunc.Exhaust(name="Gwen Stacy"))
        .SetCostFunc(CostFunc.RemoveFromGame("This"))
    ]

