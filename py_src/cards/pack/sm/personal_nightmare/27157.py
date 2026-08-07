from . import *

# Deepest Fears

def GetAbilities() -> Sequence['Ability']:

    def deepest_fears_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        size = player.hand_cards.GetSize()
        faces = player.DiscardDeckTopCards(size, effect)
        if CardFinder(card_class="IdentitySpecific").Checks(faces):
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 1, effect)
        else:
            player.GetIdentity().TakeDamage(this, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            deepest_fears_revealed
        ),
    ]

