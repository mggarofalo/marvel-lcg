from . import *

# Dragnet

def GetAbilities() -> Sequence['Ability']:

    def dragnet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        num = effect.GetPaidResources().GetColor("R")
        this.RemoveThreatFromSchemes([this], num, effect)

    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            Villain,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            dragnet
        ).SetCost(Cost("R")),
    ]

