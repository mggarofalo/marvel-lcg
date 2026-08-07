from . import *

# Spellbound

def GetAbilities() -> Sequence['Ability']:

    def spellbound_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            if player.GetIdentity().HasTrait("DEFIANT"):
                PlaceCharmCounter(player, 1, effect)
            if player.GetIdentity().HasTrait("ENTHRALLED"):
                player.DiscardControlCards(effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            spellbound_defeated,
        ),
    ]

