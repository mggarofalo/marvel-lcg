from . import *

# Psionic Surge

def GetAbilities() -> Sequence['Ability']:

    def psionic_surge_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        value = FacesCounter.GetMostCommonType(player.hand_cards.Get())

        faces = Worlds.DiscardEncounterCards(
            value,
            effect,
        )

        for face in faces:
            if face.HasTrait("PSIONIC"):
                player.DealEncounterCard(face, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            psionic_surge_revealed
        ),
    ]

