from . import *

# Serpent Solutions

def GetAbilities() -> Sequence['Ability']:

    def serpent_solutions(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        first_player.DealEncounterCard(message.trigger, effect)

    return [
        AbilityFactory.AfterCardDiscard(
            AbilityType.ForcedResponse,
            CardFinder2("SERPENT SOCIETY", Minion),
            serpent_solutions,
            from_where="EncounterDeckTop"
        ),
    ]

