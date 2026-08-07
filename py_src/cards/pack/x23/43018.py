from . import *

# * Keep Them Busy

def GetAbilities() -> Sequence['Ability']:

    def keep_them_busy(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        schemes = Worlds.GetMainSchemes(effect)
        face = player.AskChooseFace(schemes, effect)
        if face:
            player.GetIdentity().RemoveThreatFromSchemes([face], "5*", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            keep_them_busy,
            has_defeating_player=True
        ),
    ]

