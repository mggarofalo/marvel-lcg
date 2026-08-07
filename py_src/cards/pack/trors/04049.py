from . import *

# Clear the Area

def GetAbilities() -> Sequence['Ability']:

    def clear_the_area(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 2, effect)
        for target in effect.targets:
            scheme = target.CastTo(Scheme2)
            if scheme.threat == 0:
                initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            clear_the_area
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

