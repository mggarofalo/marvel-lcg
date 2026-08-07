from . import *

# * Collector

def GetAbilities() -> Sequence['Ability']:

    def get_main_scheme_stage(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        value = 0

        for scheme in Worlds.GetMainSchemes(effect):
            ui.append(scheme)
            value = scheme.printed_stage

        return value


    def collector(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        message.SetBeInstead(effect)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            scheme.RemoveThreatFromSchemes([scheme], "3*", effect)
        this.card.Flip(effect)


    return [
        AbilityFactory.ThisGainKeyword(
            get_main_scheme_stage,
            attack=1,
            scheme=1,
            change_on_event=OnEvent.MainSchemeStage(MainScheme)
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            collector
        )
    ]

