from . import *

# Bound by Business

def GetAbilities() -> Sequence['Ability']:

    def bound_by_business_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def not_share_title_in_play(face: 'CardFace') -> bool:
            faces = Worlds.GetOnFieldCards(effect)
            for check_face in faces:
                if face.IsName(check_face.name):
                    return False
            return True

        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="MARAUDER",
            check_face_fn=not_share_title_in_play,
        )
        if minion:
            minion.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bound_by_business_revealed
        ),
    ]

