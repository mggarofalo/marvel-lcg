from . import *

# Channeling Trance

def GetAbilities() -> Sequence['Ability']:

    def channeling_trance_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.area_environment.FindCards(card_type=Environment, trait="SPELL")
        if faces:
            Faces.RemoveCountersOn(faces, 1, 'invocation', effect)
        else:
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Environment,
                trait="SPELL",
            )
            if face:
                face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            channeling_trance_revealed
        ),
    ]

