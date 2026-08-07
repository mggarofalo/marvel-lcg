from . import *

# Intruder Alert!

def GetAbilities() -> Sequence['Ability']:

    def intruder_alert(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        minion = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
            trait="SENTINEL"
        )

        if minion:
            minion.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            intruder_alert,
            has_defeating_player=True
        ),
    ]

