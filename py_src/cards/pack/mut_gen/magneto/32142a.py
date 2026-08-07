from . import *

# Factory Online A

def GetAbilities() -> Sequence['Ability']:

    def factory_online_a_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'magnet', effect)

        if not Worlds.VictoryDisplay(effect).FindCard(name="Sabotage Master Mold"):
            player = Worlds.GetFirstPlayer(effect)
            minion = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                name="M-Type Sentinel",
                card_type=Minion
            )

            if minion:
                minion.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            factory_online_a_revealed
        ),
    ]

