from . import *

# Ancient Ritual

def GetAbilities() -> Sequence['Ability']:

    def ancient_ritual(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.RemoveThreatFromSchemes([this], 5, effect)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.AfterSchemePlaceThreat(
            AbilityType.ForcedResponse,
            "This",
            ancient_ritual,
            has_at_least_threat=10
        ),
    ]

