from . import *

# The Champion

def GetAbilities() -> Sequence['Ability']:

    def the_champion_revealed(effect: 'Effect', message: 'Message.AfterCardFlip') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        def action(player: 'Player'):
            player.DrawUp(1, effect)

        Players.ForEachPlayer(effect, action)

    def the_champion(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Worlds.SetGameOver(False, effect)

    return [
        AbilityFactory.AfterFlipToThisFace(
            AbilityType.ForcedResponse,
            the_champion_revealed
        ).SetName("Underdogs"),
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "10*",
            'ratings',
            the_champion
        )
    ]

