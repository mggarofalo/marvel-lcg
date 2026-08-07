from . import *

# Pirate Commander

def GetAbilities() -> Sequence['Ability']:

    def pirate_commander(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetRandomHandCards(1)
        Faces.RemoveAllFromGame(faces, effect)


    def pirate_commander_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            pirate_commander,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            pirate_commander_boost
        ),
    ]

