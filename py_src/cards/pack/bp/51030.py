from . import *

# * Dora Milaje

def GetAbilities() -> Sequence['Ability']:

    def dora_milaje(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ResolveSpecialAbility(effect.targets, effect)
        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.IgnoreThisCostIf(
            AbilityType.NonKeyword,
            your_identity_has_trait="WAKANDA"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            dora_milaje
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Ally, trait="DORA MILAJE"),
    ]

