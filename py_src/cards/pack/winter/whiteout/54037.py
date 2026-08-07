from . import *

# Whiteout

def GetAbilities() -> Sequence['Ability']:

    def whiteout_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Find.FindAndReveal(
            effect,
            player,
            name="Blizzard",
            card_type=Enemy
        )
        def action():
            ThisCardGainSurge(effect)
        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def whiteout_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
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
            whiteout_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            whiteout_boost
        ),
    ]

