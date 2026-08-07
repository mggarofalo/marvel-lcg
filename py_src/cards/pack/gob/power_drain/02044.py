from . import *

# Lightning Bolt

def GetAbilities() -> Sequence['Ability']:

    def lightning_bolt_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = Worlds.DiscardEncounterCards(2, effect)
        size = FacesCounter.CountTotalBoostIcons(faces)

        identity.TakeIndirectDamage(this, size, effect)


    def lightning_bolt_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        Worlds.DiscardEncounterCards(3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            lightning_bolt_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            lightning_bolt_boost
        ),
    ]

