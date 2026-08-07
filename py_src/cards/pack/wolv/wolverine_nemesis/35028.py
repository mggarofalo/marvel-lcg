from . import *

# * Omega Red

def GetAbilities() -> Sequence['Ability']:

    def omega_red(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        for target in message.attacked_targets:
            targets = target.GetControlByPlayer().GetControlCharacters()
            this.DealDamage(targets, 1, effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            omega_red,
        )
    ]

