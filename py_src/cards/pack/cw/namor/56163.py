from . import *

# Imperius Rex!

def GetAbilities() -> Sequence['Ability']:

    def imperius_rex_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        namor = Worlds.FindCardOnField(
            effect,
            name="Namor",
            card_type=Enemy
        )
        if namor:
            namor.DoActivate(player, effect)
        else:
            leader = Worlds.GetEnemyLeader(effect)
            if leader:
                leader.DoActivate(player, effect)

    def imperius_rex_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveActivatingEnemyAdditionalBoostCard(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            imperius_rex_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            imperius_rex_boost
        ),
    ]

