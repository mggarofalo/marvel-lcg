from . import *

# Chaos Manipulation

def GetAbilities() -> Sequence['Ability']:

    def chaos_manipulation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Luminous",
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(player, effect)

        faces = Worlds.DiscardEncounterCards(1, effect)
        value = FacesCounter.CountTotalBoostIcons(faces)
        if value >= 2:
            face = Worlds.FindCardOnField(
                effect,
                name="Luminous",
                card_type=Minion
            )
            if face:
                face.DoActivate(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            chaos_manipulation_revealed
        ),
    ]

