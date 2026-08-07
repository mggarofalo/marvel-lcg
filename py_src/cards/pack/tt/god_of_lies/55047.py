from . import *

# Lofty Goals

def GetAbilities() -> Sequence['Ability']:

    def lofty_goals(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        PlaceShatterCountersOnTheAvatarOfLokivillain(2)

    def lofty_goals_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        damage = len(player.GetControlCharacters())
        identity.TakeIndirectDamage(this, damage, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            lofty_goals,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            lofty_goals_boost
        ),
    ]

