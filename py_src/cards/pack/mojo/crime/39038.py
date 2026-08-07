from . import *

# Law & Order

def GetAbilities() -> Sequence['Ability']:

    def law_order(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        num = effect.GetPaidResources().GetColor("Y")
        this.RemoveThreatFromSchemes([this], num, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Friend,
            attack=-2,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            law_order
        ).SetCost(Cost("Y")),
    ]

