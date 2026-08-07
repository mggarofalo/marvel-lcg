from . import *

# Crime Scene Investigation

def GetAbilities() -> Sequence['Ability']:

    def crime_scene_investigation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        num = effect.GetPaidResources().GetColor("B")
        this.RemoveThreatFromSchemes([this], num, effect)


    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            Scheme2,
            not_from_this_effect=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            crime_scene_investigation
        ).SetCost(Cost("B")),
    ]

