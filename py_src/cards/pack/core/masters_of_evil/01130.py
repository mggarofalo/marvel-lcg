from . import *

# * Whirlwind

def GetAbilities() -> Sequence['Ability']:

    def whirlwind(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        # Q: How is Whirlwind’s attack against each hero
        # resolved?
        # A: Whirlwind makes a separate attack against each hero
        # and these attacks are resolved simultaneously. Each player
        # resolves each step of the attack simultaneously. Each attack
        # may have a defender declared, and each defender defends
        # for a single player.
        units = Worlds.GetOnFieldHeroes(effect)
        message.AlsoResolveThisAttackTo(units)

    def whirlwind_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        this.DealDamage(Worlds.GetOnFieldHeroes(effect), 1, effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            whirlwind,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            whirlwind_boost
        ),
    ]

