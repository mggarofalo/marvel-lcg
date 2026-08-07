from . import *

# * Mikhail Rasputin

def GetAbilities() -> Sequence['Ability']:

    def mikhail_rasputin(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        identity = player.GetIdentity()

        this.DealDamage([identity], 1, effect)


    return [
        AbilityFactory.ThisMinionEngageFirstPlayer(),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            mikhail_rasputin
        ),
    ]

