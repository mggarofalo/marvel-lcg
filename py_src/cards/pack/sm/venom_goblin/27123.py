from . import *

# Symbiotic Thrall

def GetAbilities() -> Sequence['Ability']:

    def symbiotic_thrall_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        for scheme in Worlds.GetMainSchemes(effect):
            if scheme.GetCounters('glider') == 0:
                this.PlaceThreatOnSchemes([scheme], 1, effect)

    return [
        AbilityFactory.ThisGainKeywordWhileFaceIsInPlay(
            CardFinder2("SYMBIOTE", Environment),
            patrol=1,
            change_on_event=OnEvent.Trait(EncounterCard)
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            symbiotic_thrall_boost
        )
    ]

