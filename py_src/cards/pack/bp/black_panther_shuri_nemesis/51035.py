from . import *

# M.U.S.I.C.

def GetAbilities() -> Sequence['Ability']:

    def music_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        units = player.GetControlCharacters(CardFinder(is_stunned=False))
        stunned_units = player.GetControlCharacters(CardFinder(is_stunned=True))

        Faces.GiveStatus(units, "Stunned", effect)
        this.DealDamage(stunned_units, 1, effect)

    def music_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            identity.TakeDamage(this, 1, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            music_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            music_boost
        ),
    ]

