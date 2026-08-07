from . import *

# * Thor: Odinson

def GetAbilities() -> Sequence['Ability']:

    def thor(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.AddTarget(*effect.targets)

    def select_minion_engaged_with_that_player(effect: 'Effect'):
        message = effect.GetBindMessage(Message.WhenUnitWouldAttack)
        minions: List['Minion'] = []
        for target in message.attacked_targets:
            if Minion.IsType(target) and target.engaged_player:
                for minion in target.engaged_player.GetEngagedMinions():
                    if minion != target:
                        minions.append(minion)
        return minions


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            thor,
            attack_targets=Minion,
        ).SetTarget(select_minion_engaged_with_that_player, range="All")
        .SetCost(Cost("Y")),
    ]

