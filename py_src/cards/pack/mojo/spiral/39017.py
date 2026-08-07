from . import *

# Cornered!

def GetAbilities() -> Sequence['Ability']:

    def cornered_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect, name="Spiral")
        if villain:
            villain.FlipTo(effect, trait="CORNERED")

        face = GetShowDeck(effect).GetTopCard()
        if face:
            face.Reveal(player, effect)
        Faces.ShuffleAllTo([this], GetShowDeck(effect).deck, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            cornered_revealed
        ).SetCannotBeCancel(),
    ]

