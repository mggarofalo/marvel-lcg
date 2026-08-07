from . import *

# Overwhelming Force

def GetAbilities() -> Sequence['Ability']:

    def overwhelming_force_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = player.DiscardControlCards(effect, upgrade=True, support=True, highest_cost=True)
        if not face:
            ThisCardGainSurge(effect)


    def overwhelming_force_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            overwhelming_force_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            overwhelming_force_boost
        ),
    ]

