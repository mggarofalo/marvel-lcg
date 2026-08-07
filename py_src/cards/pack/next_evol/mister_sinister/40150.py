from . import *

# Sinister Strike

def GetAbilities() -> Sequence['Ability']:

    def sinister_strike_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        villain = FindMisterSinister(effect)
        if villain:
            villain.DoAttackYou(player, effect)
            if villain.HasTrait("AERIAL"):
                Faces.GiveStatus([identity], "Stunned", effect)


    def sinister_strike_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            sinister_strike_hero
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sinister_strike_boost
        ),
    ]

