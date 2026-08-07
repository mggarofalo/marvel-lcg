from . import *

# Uncanny Resilience

def GetAbilities() -> Sequence['Ability']:

    def uncanny_resilience(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        removed = False
        for villain in Worlds.GetVillains(effect):
            removed |= villain.DiscardStunned(effect, rule="All")
            removed |= villain.DiscardConfused(effect, rule="All")
        if not removed:
            this.GainSurge(1, effect)

    def you_are_confused(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        identity = message.GetToPlayer().GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            uncanny_resilience
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            you_are_confused
        )
    ]

