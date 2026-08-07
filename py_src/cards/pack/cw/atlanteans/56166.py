from . import *

# Atlanteans

def GetAbilities() -> Sequence['Ability']:

    def atlanteans_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        faces = player.DiscardDeckTopCards(3, effect)

        size = FacesCounter.GetDifferentTypesCount(faces)

        if player.IsAlterEgo():
            this.PlaceThreatOnSchemes("MainScheme", size, effect)
        if player.IsInHeroForm():
            identity.TakeIndirectDamage(this, size, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            atlanteans_revealed
        ),
    ]

