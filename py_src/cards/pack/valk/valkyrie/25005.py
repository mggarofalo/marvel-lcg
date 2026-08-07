from . import *

# * Valkyrie's Spear

def GetAbilities() -> Sequence['Ability']:

    def valkyries_spear(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainDEFForThisAttack(+1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Valkyrie"),
            defense=1,
        ),
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.NonKeyword,
            "You",
            valkyries_spear,
            against_who=HAS_DEATH_GLOW_FINDER,
        ),
    ]

