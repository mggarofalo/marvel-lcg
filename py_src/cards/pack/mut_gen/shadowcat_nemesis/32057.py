from . import *

# The Hellfire Club

def GetAbilities() -> Sequence['Ability']:

    def the_hellfire_club(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        minion = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            include_set_aside=True,
            name="Hellfire Pawn",
            card_type=Minion
        )
        if minion:
            minion.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_hellfire_club,
            has_defeating_player=True
        ),
    ]

