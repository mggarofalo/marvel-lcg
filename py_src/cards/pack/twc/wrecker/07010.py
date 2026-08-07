from . import *

# Buddy System

def GetAbilities() -> Sequence['Ability']:

    def buddy_system(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)
        villain = scheme.FindCorrespondingVillain()

        if villain:
            # Reveal the top card of his deck (top 2 cards instead if he is the only villain in play).
            villain.encounter_deck.RevealTopCard(player, effect)

            if len(Worlds.GetVillains(effect)) == 1:
                villain.encounter_deck.RevealTopCard(player, effect)

    def buddy_system_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)
        villain = scheme.FindCorrespondingVillain()
        if villain:
            villain.SetActive(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            buddy_system
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            buddy_system_boost
        )
    ]

