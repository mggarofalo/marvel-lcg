from . import *

# Abandoned Facility

def GetAbilities() -> Sequence['Ability']:

    def abandoned_facility(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()

        scheme = Worlds.FindMainScheme(this)
        if scheme and scheme.GetCounters('delay') >= 5:
            value = 2
        else:
            value = 1
        Players.DiscardResourceIconFromHand(player, value, effect)

    return [
        AfterAbsorbingManMakesUndefendedAttack(abandoned_facility),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]

