from . import *

# True Grit

def GetAbilities() -> Sequence['Ability']:

    def true_grit(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        hero = message.defender
        if Hero.IsType(hero):
            this.RemoveThreatFromSchemes(effect.targets, hero.thwart, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "YourHero",
            true_grit,
            attacker=Enemy
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

