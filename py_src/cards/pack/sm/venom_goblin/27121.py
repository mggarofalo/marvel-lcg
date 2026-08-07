from . import *

# Symbiotic Berserker

def GetAbilities() -> Sequence['Ability']:

    def symbiotic_berserker_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)

        player = message.GetToPlayer()
        scheme = MoveGliderToTheMainSchemeWithMostThreat(player, effect)
        this.PlaceThreatOnSchemes([scheme], 1, effect)

        # Hack
        # if type(message.being_message) is Message.WhenSchemeBeingScheme:
        #     message.would_message.ReplaceTarget(scheme)

    return [
        AbilityFactory.ThisGainKeywordWhileFaceIsInPlay(
            CardFinder2("SYMBIOTE", Environment),
            quickstrike=1,
            change_on_event=OnEvent.Trait(EncounterCard)
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            symbiotic_berserker_boost
        )
    ]

