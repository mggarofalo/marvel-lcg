from . import *

# Batroc the Leaper

def GetAbilities() -> Sequence['Ability']:

    def batroc_the_leaper_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            ThisCardGainSurge(effect)

        face = Find.FindAndReveal(
            effect,
            player,
            name="Batroc",
            card_type=Enemy
        )
        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def batroc_the_leaper_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            batroc_the_leaper_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            batroc_the_leaper_boost
        ),
    ]

