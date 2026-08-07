from . import *

# Prison Camps

def GetAbilities() -> Sequence['Ability']:

    def prison_camps(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.PlayerCard(
            effect,
            player,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Ally,
        )
        if face:
            face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            prison_camps,
            has_defeating_player=True
        ),
    ]

