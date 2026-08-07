from . import *

# Bushwhack

def GetAbilities() -> Sequence['Ability']:

    def bushwhack(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        minion = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            trait="MARAUDER",
            card_type=Minion,
        )

        if minion:
            minion.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            bushwhack,
            has_defeating_player=True
        ),
    ]

