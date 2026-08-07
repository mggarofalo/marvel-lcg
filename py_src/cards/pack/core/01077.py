from . import *

# Counter-Punch

def GetAbilities() -> Sequence['Ability']:

    def counter_punch(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        this.DealDamage(effect.targets, hero.attack, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "YourHero",
            counter_punch,
            attacker=Enemy,
        ).SetPlay().SetLabel('attack')
        .SetTarget("Attacker"),
    ]

