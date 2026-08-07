from . import *

# * Zola (III)

def GetAbilities() -> Sequence['Ability']:

    def zola_iii(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                card_type=Minion,
            )
            if face:
                face.Reveal(player, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            zola_iii
        ),
    ]

