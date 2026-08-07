from . import *

# Political Retribution

def GetAbilities() -> Sequence['Ability']:

    def political_retribution_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        enemy = Worlds.FindCardOnField(
            effect,
            name="Lucia von Bardas",
            card_type=Enemy
        )
        if enemy:
            enemy.DoSchemes(player, effect)
        scheme = Worlds.FindCardOnField(
            effect,
            name="Rule by Force",
            card_type=Scheme2
        )
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 3, effect)
        if not enemy and not scheme:
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            political_retribution_revealed
        ),
    ]

