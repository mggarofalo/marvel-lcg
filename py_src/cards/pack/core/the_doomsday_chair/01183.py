from . import *

# The Doomsday Chair

def GetAbilities() -> Sequence['Ability']:

    def the_doomsday_chair_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="M.O.D.O.K."
        )

        if not face:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="M.O.D.O.K.",
                card_type=Minion
            )
            if face:
                face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_doomsday_chair_revealed
        ),
    ]

