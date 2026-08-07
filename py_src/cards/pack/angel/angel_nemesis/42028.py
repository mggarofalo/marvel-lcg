from . import *

# Spear Shot

def GetAbilities() -> Sequence['Ability']:

    def spear_shot_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = player.DiscardDeckUntil(
            effect,
            card_type=Event
        )
        if face:
            identity = player.GetIdentity()
            damage = FacesCounter.GetPrintedCost([face])
            identity.TakeIndirectDamage(this, damage, effect)

            if face.HasTrait("AERIAL"):
                ThisCardGainSurge(effect)

    def spear_shot_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        identity.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spear_shot_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            spear_shot_boost
        ),
    ]

