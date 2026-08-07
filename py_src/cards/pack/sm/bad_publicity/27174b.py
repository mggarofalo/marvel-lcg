from . import *

# Public Outcry

def GetAbilities() -> Sequence['Ability']:

    def public_outcry(effect: 'Effect', message: 'Message.AfterUnitBeDefeated|Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'notoriety', effect)

    return [
        AbilityFactory.ExpertModeOnly(),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.Response,
            Minion,
            public_outcry
        ),
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.Response,
            SchemeSide2,
            public_outcry
        ),
    ]

