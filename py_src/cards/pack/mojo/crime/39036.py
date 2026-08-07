from . import *

# Build the Case

def GetAbilities() -> Sequence['Ability']:

    def build_the_case(effect: 'Effect', message: 'Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Obligation)

        Faces.PlaceCountersOn([this], 1, 'clue', effect)
        if this.GetCounters('clue') >= 3:
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.ForcedResponse,
            SchemeSide2,
            build_the_case
        )
    ]

