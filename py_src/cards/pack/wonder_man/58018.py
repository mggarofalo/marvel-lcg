from . import *

# Everywhere All at Once

def GetAbilities() -> Sequence['Ability']:

    def everywhere_all_at_once(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        paid = effect.GetPaidResources().GetResourceIconTypes()
        if paid > len(effect.targets):
            value = 3
        else:
            value = 2
        targets = effect.targets[0:paid]
        this.RemoveThreatFromSchemes(targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            everywhere_all_at_once
        ).SetPlay(only_if_your_identity_has_trait="AERIAL").SetLabel('thwart')
        .SetTarget(Scheme2, range=(1, "All"))
    ]

