from . import *

# Techno

def GetAbilities() -> Sequence['Ability']:

    def techno_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Find.FindAndReveal(
            effect,
            player,
            name="Fixer",
            card_type=Enemy
        )
        def action():
            ThisCardGainSurge(effect)
        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def techno_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.would_atk_message.GainPiercing(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            techno_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            techno_boost,
            during_attack=True
        ),
    ]

