from . import *

# Photon Blasters

def GetAbilities() -> Sequence['Ability']:

    def photon_blasters(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        value = GetIronheartVersionNumber(initiator)
        this.DealDamage(effect.targets, value, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=2,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            photon_blasters
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

