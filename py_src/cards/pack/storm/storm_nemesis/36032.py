from . import *

# Leader of the Morlocks

def GetAbilities() -> Sequence['Ability']:

    def leader_of_the_morlocks(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            include_set_aside=True,
            name="Knife Fight",
            card_type=Treachery
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            leader_of_the_morlocks,
            has_defeating_player=True
        ),
    ]

