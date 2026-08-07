from . import *

# Lower Manhattan

def GetAbilities() -> Sequence['Ability']:

    def lower_manhattan(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        MoveGliderToTheMainSchemeWithLeastThreat(player, effect, True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            lower_manhattan
        ),
        LoseTheGameIfAtLeast2SymbioteEnviromentsInPlay()
    ]

