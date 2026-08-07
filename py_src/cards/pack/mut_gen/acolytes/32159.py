from . import *

# * Fabian Cortez

def GetAbilities() -> Sequence['Ability']:

    def fabian_cortez(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion,
                trait="ACOLYTE"
            )
            if minion:
                minion.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            fabian_cortez
        ),
    ]

