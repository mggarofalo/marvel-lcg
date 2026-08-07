from . import *

# Oversized Hands

def GetAbilities() -> Sequence['Ability']:

    def oversized_hands(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        asset = player.DiscardControlCards(effect, support=True, highest_cost=True)
        if not asset:
            villain = Worlds.FindVillain(effect)
            if villain:
                scheme = villain.FindCorrespondingScheme()
                if scheme:
                    this.PlaceThreatOnSchemes([scheme], 2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            oversized_hands
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            DiscardSelectedCards
        ).SetTarget(Support, from_where=["YouControlCards"])
    ]

