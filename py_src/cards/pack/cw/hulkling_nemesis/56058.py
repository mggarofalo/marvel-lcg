from . import *

# You're Coming With Me!

def GetAbilities() -> Sequence['Ability']:

    def youre_coming_with_me_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.DiscardDeckTopCards(3, effect)
        size = FacesCounter.GetPrintedResourcesTypes(faces)
        this.PlaceThreatOnSchemes("MainScheme", size, effect)

    def youre_coming_with_me_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        faces = player.DiscardDeckTopCards(3, effect)
        size = FacesCounter.GetPrintedResourcesTypes(faces)
        identity.TakeDamage(this, size, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            youre_coming_with_me_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            youre_coming_with_me_hero
        ),
    ]

