from . import *

# * Apocalypse

def GetAbilities() -> Sequence['Ability']:

    def apocalypse(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.SetBeInstead(effect)

        this.RemoveThreatFromSchemes([message.trigger], "All", effect, ignore_crisis=True)
        Faces.RemoveAllFromGame([this], effect)
        SetupCards.Reveal(
            effect,
            name="Apocalypse"
        )

    return [
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.ForcedInterrupt,
            MainScheme,
            apocalypse
        ),
    ]

