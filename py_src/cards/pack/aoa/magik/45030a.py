from . import *

# * Magik

def GetAbilities() -> Sequence['Ability']:

    MAGIK_ONCE_PER_PHASE = "MAGIK_ONCE_PER_PHASE"

    def magik_play_card(effect: 'Effect', message: 'Message.WhenPlayerPlayCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        if message.from_area == initiator.player_deck:
            initiator.flag.Update(MAGIK_ONCE_PER_PHASE, +1)

            RunAt.TheEndOfThePhase(
                effect,
                lambda:
                    initiator.flag.Update(MAGIK_ONCE_PER_PHASE, -1)
            )

    def magik_condition(effect: 'Effect', message: 'Message.CheckIfFaceIsLikeInHand') -> bool:
        initiator = effect.GetInitiator()

        return initiator.flag.Get(MAGIK_ONCE_PER_PHASE) == 0

    def magik_set_like_in_hand(face: 'CardFace', effect: 'Effect') -> None:
        initiator = effect.GetInitiator()
        face.card.can_state.is_like_in_hand = None
        for face in initiator.player_deck.GetAll():
            if face.card.can_state.is_like_in_hand == True:
                face.card.can_state.is_like_in_hand = None

    return [
        *AbilityFactory.YouMayPlayCardLikeInHand(
            AbilityType.NonKeyword,
            None,
            from_where="YourDeckTop",
            conditions=[magik_condition]
        ),
        AbilityFactory.WhenPlayerPlayCard(
            AbilityType.NonKeyword,
            "You",
            None,
            magik_play_card,
        ),
        AbilityFactory.ReduceCostToPlayFaceWhen(
            CardFinder(check_face_fn=lambda face: face.card.area.flags.is_player_deck),
            1,
            "You",
        ),
        *AbilityFactory.PlayWithTopCardOfDeckFaceup(
            lambda effect:
                [effect.GetInitiator().player_deck],
            ex_operation=magik_set_like_in_hand
        )
    ]

