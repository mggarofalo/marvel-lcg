from . import *

# Celestial Tech

def GetAbilities() -> Sequence['Ability']:

    def celestial_tech_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        has_attached = False

        villain = Worlds.FindVillain(effect)
        if villain:
            faces = villain.GetInventoryDeck().FindCards(trait="CELESTIAL", card_type=Attachment)
            if faces:
                has_attached = True
                Faces.ResolveAbility(faces, player, AbilityType.ForcedInterrupt, effect)

        if not has_attached:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                trait="CELESTIAL",
                card_type=Attachment
            )
            if face:
                face.Reveal(None, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            celestial_tech_revealed
        ),
    ]

