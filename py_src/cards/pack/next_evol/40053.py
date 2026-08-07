from . import *

# Team Investigation

def GetAbilities() -> Sequence['Ability']:

    def team_investigation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, "3*", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            team_investigation
        ).SetPlay().SetLabel()
        .SetTarget(SchemeSide2),
    ]

