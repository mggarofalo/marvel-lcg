from . import *

# Giant Help

def GetAbilities() -> Sequence['Ability']:

    def giant_help(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    def giant_help_divided(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            giant_help,
            conditions=[
                lambda effect, message:
                    not Condition.YouAreInHeroFrom(effect, "GIANT"),
            ]
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            giant_help_divided,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "GIANT"),
            ]
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2, range=(1, 4), repeat_rules="Threat"),
    ]

