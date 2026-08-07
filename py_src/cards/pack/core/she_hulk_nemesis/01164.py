from . import *

# Titania's Fury

def GetAbilities() -> Sequence['Ability']:

    def titanias_fury_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minion = Worlds.FindCardOnField(
            effect,
            name="Titania",
            card_type=Minion
        )
        if minion and player.IsHero() and minion.DoAttackYou(player, effect):
            pass
        else:
            if minion:
                this.HealthUnits([minion], "All", effect)
            ThisCardGainSurge(effect)


    def titanias_fury_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            titanias_fury_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            titanias_fury_boost
        ),
    ]

