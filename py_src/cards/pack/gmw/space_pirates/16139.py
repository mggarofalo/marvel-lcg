from . import *

# Pirate Lackey

def GetAbilities() -> Sequence['Ability']:

    def pirate_lackey(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.player_deck.Get(True)
        if not faces:
            # "Remove the top card of your deck from the game" -- an empty deck
            # has no top card, so there is nothing to remove. Indexing it raised
            # IndexError mid-game and mid-replay (MARVEL-158).
            return
        Faces.RemoveAllFromGame([faces[0]], effect)


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

