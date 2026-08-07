from . import *

# * Lady Deathstrike

def GetAbilities() -> Sequence['Ability']:

    def lady_deathstrike(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            face = Worlds.DiscardEncounterTopCard(effect)
            if face:
                damage = FacesCounter.CountTotalBoostIcons([face])
                identity = player.GetIdentity()
                identity.TakeIndirectDamage(this, damage, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            lady_deathstrike
        ),
    ]

