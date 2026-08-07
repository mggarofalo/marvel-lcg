from . import *

# * Red Skull

def GetAbilities() -> Sequence['Ability']:

    def red_skull_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)


    return [
        RedSkullGetsATKForEachSideSchemeInPlay(),
        AbilityFactory.WhenThisRevealed(
            None,
            red_skull_revealed
        ),
    ]

