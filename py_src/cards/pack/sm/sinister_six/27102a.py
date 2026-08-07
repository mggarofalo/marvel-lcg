from . import *

# Light at the End

def GetAbilities() -> Sequence['Ability']:

    def light_at_the_end(effect: 'Effect', message: 'Message.AfterCardRemovedToken') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        ResolveMainSchemeAmbush(first_player, effect)

        this.card.Flip(effect)


    return [
        ThePlayersCannotWinUnlessTheyEscap(),
        AbilityFactory.WhenLastThreatRemoveFromThisScheme(
            AbilityType.ForcedInterrupt,
            light_at_the_end
        ),
    ]

