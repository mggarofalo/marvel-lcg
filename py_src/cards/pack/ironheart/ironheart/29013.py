from . import *

# Propulsion Jets

def GetAbilities() -> Sequence['Ability']:

    def propulsion_jets(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        value = GetIronheartVersionNumber(initiator)
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=2,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            propulsion_jets
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
    ]

