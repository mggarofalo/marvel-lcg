from . import *

# Rocket Boots

def GetAbilities() -> Sequence['Ability']:

    def rocket_boots(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetIdentity()

        hero.GainUntilPhaseEnd(effect, trait="AERIAL")


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=1,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            rocket_boots
        ).SetCost(Cost("B"))
        .SetCostFunc(CostFunc.Exhaust("This"))
    ]
