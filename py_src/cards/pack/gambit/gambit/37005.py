from . import *

# * Gambit's Guild Armor

def GetAbilities() -> Sequence['Ability']:

    def gambits_guild_armor(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            CardFinder(name="Gambit"),
            gambits_guild_armor,
            take_no_damage=True
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("DefendingCharacter", canbe_ready=True),
    ]

