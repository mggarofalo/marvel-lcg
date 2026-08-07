from . import *

# * M.U.S.I.C.

def GetAbilities() -> Sequence['Ability']:

    def music_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        Find.FindAndPutIntoPlay(
            effect,
            player,
            name="Manipulated M.U.S.I.C."
        )

    def music(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Manipulated M.U.S.I.C."
        )
        if face:
            scheme = Worlds.FindMainScheme(effect)
            if scheme:
                value = this.RemoveThreatFromSchemes([face], "All", effect)
                if value:
                    this.PlaceThreatOnSchemes([scheme], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            music_revealed
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            music
        ),
    ]

