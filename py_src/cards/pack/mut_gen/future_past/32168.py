from . import *

# Nimrod's Portal

def GetAbilities() -> Sequence['Ability']:

    def nimrods_portal(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = Worlds.DiscardEncounterCards(2, effect)
            value = FacesCounter.CountTotalBoostIcons(faces)
            identity = player.GetIdentity()
            identity.TakeIndirectDamage(this, value, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            nimrods_portal,
        ),
    ]

