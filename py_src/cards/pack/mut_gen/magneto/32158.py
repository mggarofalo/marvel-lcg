from . import *

# Seized!

def GetAbilities() -> Sequence['Ability']:

    def seized_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = player.player_deck.GetTops(6)
            this.PlaceCardHere(faces, False, effect)
        Players.ForEachPlayer(effect, action)

    def seized_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        magneto = Worlds.FindCardOnField(
            effect,
            name="Magneto",
            card_type=Enemy
        )

        if magneto:
            magneto.DoActivate(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            seized_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            seized_defeated,
            has_defeating_player=True
        ),
    ]

