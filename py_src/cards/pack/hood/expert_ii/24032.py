from . import *

# Slug It Out

def GetAbilities() -> Sequence['Ability']:

    def slug_it_out_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.ExhaustAll([identity], effect)
        identity.TakeDamage(this, 2, effect)


    def slug_it_out_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)
        message.GiveBoostCardForThisActivation(Villain, 1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            slug_it_out_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            slug_it_out_boost
        ),
    ]

