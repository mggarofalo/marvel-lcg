from . import *

# * Absorbing Man

def GetAbilities() -> Sequence['Ability']:

    def absorbing_man_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Super Absorbing Power",
            card_type=SchemeSide2
        )
        if face:
            def action(player: 'Player'):
                player.DealEncounterCards(1, effect)
            Players.ForEachPlayer(effect, action)
        else:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Super Absorbing Power",
            )
            if face:
                face.Reveal(None, effect)


    return [
        *AbsorbingManGainsTraitOfEnvironment(),
        AbilityFactory.WhenThisRevealed(
            None,
            absorbing_man_revealed
        ),
    ]

