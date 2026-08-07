from . import *

# Growing Strong

def GetAbilities() -> Sequence['Ability']:

    def growing_strong_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Find.FindAndReveal(
            effect,
            player,
            name="Atlas",
            card_type=Enemy
        )
        def action():
            ThisCardGainSurge(effect)

        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def growing_strong_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        Faces.GiveStatus([message.activating_enemy], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            growing_strong_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            growing_strong_boost
        ),
    ]

