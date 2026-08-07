from . import *

# 'Pool Inspection

def GetAbilities() -> Sequence['Ability']:

    def pool_inspection(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"])

        this.RemoveThreatFromSchemes(effect.targets, 5, effect)
        this.RemoveThreatFromSchemes("EachScheme", value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            pool_inspection
        ).SetPlay().SetLabel('thwart')
        .SetTarget(MainScheme)
        .SetIgnoreKeyword('Crisis'),
    ]

