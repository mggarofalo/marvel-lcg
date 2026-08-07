from . import *

# Goblin

def GetAbilities() -> Sequence['Ability']:

    def goblin_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        faces = Worlds.GetOnFieldSchemes(effect, finder=CardFinder(has_threat=True))
        face = first_player.AskChooseFace(faces, effect)
        if face:
            this.RemoveThreatFromSchemes([face], 2, effect)


    return [
        AbilityFactory.UnitTakeAdditionalDamageFromCards(
            "This",
            "Prevent",
            CardFinder(non_printed_res="R"),
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            goblin_defeated
        ),
    ]

