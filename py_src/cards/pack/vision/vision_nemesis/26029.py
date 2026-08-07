from . import *

# * Ultron

def GetAbilities() -> Sequence['Ability']:

    def ultron(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        CreateUltronFacedownDrone(player, 1, effect)

    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            ultron,
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="Ultron Drones"
                    ) != None
            ]
        ),
    ]

