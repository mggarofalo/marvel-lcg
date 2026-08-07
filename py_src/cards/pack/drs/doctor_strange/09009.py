from . import *

# * Cloak of Levitation

def GetAbilities() -> Sequence['Ability']:

    def cloak_of_levitation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="AERIAL",
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cloak_of_levitation,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Doctor Strange", canbe_ready=True)
    ]

