from . import *

# Burn!

def GetAbilities() -> Sequence['Ability']:

    def burn_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        value = 2
        pyro = Worlds.FindCardOnField(
            effect,
            name="Pyro"
        )
        if pyro:
            value = 3
        faces = player.DiscardDeckTopCards(value, effect)

        damage = FacesCounter.GetPrintedResourcesIcon(faces, player.player_deck)
        identity.TakeIndirectDamage(this, damage, effect)

    def burn_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = player.DiscardDeckTopCard(effect)

        value = FacesCounter.GetPrintedResourcesIcon([face], player.player_deck)

        message.GainBoostIcon(
            value,
            effect
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            burn_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            burn_boost
        ),
    ]

