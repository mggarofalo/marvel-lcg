from . import *

# "Stop Hitting Yourself"

def GetAbilities() -> Sequence['Ability']:

    def stop_hitting_yourself(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetHero().defense
        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "You",
            stop_hitting_yourself,
            attacker=Enemy,
            take_no_damage=True,
        ).SetPlay().SetLabel('attack')
        .SetTarget("Attacker"),
    ]

