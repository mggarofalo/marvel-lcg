from . import *

# Swarm Attack

def GetAbilities() -> Sequence['Ability']:

    def swarm_attack(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        player = message.GetToPlayer()

        made_attack = False
        if player.IsHero():
            for unit in player.GetEngagedMinions():
                if unit.HasTrait("DRONE"):
                    if unit.DoAttackYou(player, effect):
                        made_attack = True

        if not made_attack:
            CreateUltronFacedownDrone(player, 1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            swarm_attack
        ),
    ]
