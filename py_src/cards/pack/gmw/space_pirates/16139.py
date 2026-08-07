from . import *

# Pirate Lackey

def GetAbilities() -> Sequence['Ability']:

    def pirate_lackey(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = player.player_deck.Get(True)[0]
        Faces.RemoveAllFromGame([face], effect)


    def pirate_lackey_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedInterrupt,
            "This",
            "You",
            pirate_lackey,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            pirate_lackey_boost
        ),
    ]

