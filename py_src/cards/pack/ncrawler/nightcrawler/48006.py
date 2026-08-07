from . import *

# Bamf!

def GetAbilities() -> Sequence['Ability']:

    def bamf(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        # where “declaring X the defender without exhausting” encompasses the character making a basic defense and using their DEF to reduce damage dealt.
        hero = effect.targets[0].CastTo(Hero)
        message.DeclareDefender(hero, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Enemy),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            "AttachedEnemy",
            bamf
        ).SetLabel('defense')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(name="Nightcrawler"),
    ]

