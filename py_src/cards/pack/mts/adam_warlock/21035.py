from . import *

# * Warlock's Cape

def GetAbilities() -> Sequence['Ability']:

    def warlocks_cape(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.HeroResponse,
            "You",
            CardFinder(name="Adam Warlock"),
            warlocks_cape,
            ability_name="Battle Mage",
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Adam Warlock", canbe_ready=True),
    ]

