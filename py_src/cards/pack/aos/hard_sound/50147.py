from . import *

# Hard Sound

def GetAbilities() -> Sequence['Ability']:

    def hard_sound_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Find.FindAndReveal(
            effect,
            player,
            name="Songbird",
            card_type=Enemy,
        )

        def action():
            ThisCardGainSurge(effect)

        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def hard_sound_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveActivatingEnemyAdditionalBoostCard(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hard_sound_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hard_sound_boost
        ),
    ]

