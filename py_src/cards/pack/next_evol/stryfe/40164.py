from . import *

# * Stryfe

def GetAbilities() -> Sequence['Ability']:

    def stryfe_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Attachment,
                trait="PSIONIC",
            )
            if face:
                face.Reveal(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        Stryfe(),
        AbilityFactory.WhenThisRevealed(
            None,
            stryfe_revealed
        ),
    ]

