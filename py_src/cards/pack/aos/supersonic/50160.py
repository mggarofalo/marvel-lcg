from . import *

# Supersonic

def GetAbilities() -> Sequence['Ability']:

    def supersonic_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            ThisCardGainSurge(effect)

        face = Find.FindAndReveal(
            effect,
            player,
            name="MACH-IV",
            card_type=Enemy
        )
        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def supersonic_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.would_atk_message.GainOverKill(effect)
        message.would_atk_message.GainRanged(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            supersonic_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            supersonic_boost,
            during_attack=True,
        ),
    ]

