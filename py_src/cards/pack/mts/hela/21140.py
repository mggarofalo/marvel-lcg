from . import *

# Gnipahellir

def GetAbilities() -> Sequence['Ability']:

    def gnipahellir(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        face_names = ["Gjallerbru", "Skurge"]
        for name in face_names:
            face = Worlds.AsideDeck(effect).FindCard(name=name)
            if face:
                face.Reveal(first_player, effect)

        def action(player: 'Player'):
            if player != first_player:
                player.DealEncounterCards(1, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            gnipahellir
        ),
    ]

