from . import *

# * Boomerang

def GetAbilities() -> Sequence['Ability']:

    def boomerang(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlAllies(), 1, effect)


    def boomerang_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        allies = player.GetControlAllies()
        face = player.AskChooseFace(allies, effect)
        if face:
            this.DealDamage([face], 2, effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            "You",
            boomerang
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            boomerang_boost
        ),
    ]

