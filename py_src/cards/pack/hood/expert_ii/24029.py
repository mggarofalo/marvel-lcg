from . import *

# Cruel Intentions

def GetAbilities() -> Sequence['Ability']:

    def cruel_intentions_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveFacedownBoostCards([villain], 1, effect)


    def cruel_intentions_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DealEncounterCards(1, effect)


    return [

        AbilityFactory.WhenThisRevealed(
            None,
            cruel_intentions_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            cruel_intentions_boost
        ),
    ]

