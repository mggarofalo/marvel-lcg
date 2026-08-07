from . import *

# Tactical Prowess

def GetAbilities() -> Sequence['Ability']:

    def tactical_prowess(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        least_scheme = Worlds.FilterSideScheme(effect, least_threat=True)
        most_scheme = Worlds.FilterSideScheme(effect, most_threat=True)

        if least_scheme and most_scheme:
            threat = least_scheme.threat
            least_scheme.RemoveThreatFromSchemes([least_scheme], threat, effect)

            def action():
                this.GainSurge(1, effect)

            this.PlaceThreatOnSchemes([most_scheme], threat, effect, if_not_trigger_forced_response=action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tactical_prowess
        )
    ]

