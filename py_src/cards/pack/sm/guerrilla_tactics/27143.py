from . import *

# Coordinated Effort

def GetAbilities() -> Sequence['Ability']:

    def coordinated_effort_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        if Worlds.IsExpert(effect):
            scheme = Worlds.FindMainScheme(this)
            schemes = Worlds.GetOnFieldSchemes(effect)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 2, effect)
                schemes.remove(scheme)
            this.PlaceThreatOnSchemes(schemes, 1, effect)
        else:
            schemes = Worlds.GetOnFieldSchemes(effect)
            this.PlaceThreatOnSchemes(schemes, 1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            coordinated_effort_boost
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Enemy,
            acceleration_icon=1,
        ),
    ]

