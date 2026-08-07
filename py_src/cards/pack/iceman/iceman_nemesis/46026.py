from . import *

# Playing with Fire

def GetAbilities() -> Sequence['Ability']:

    def playing_with_fire(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        faces = player.DiscardDeckTopCards(3, effect)
        damage = FacesCounter.GetPrintedResourcesIcon(faces, player.player_deck)
        player.GetIdentity().TakeIndirectDamage(this, damage, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            playing_with_fire,
            has_defeating_player=True
        ),
    ]

