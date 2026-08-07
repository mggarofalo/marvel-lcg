from . import *

# Mutant Slayers

def GetAbilities() -> Sequence['Ability']:

    def mutant_slayers_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.GetOnFieldCharacters(effect)
        faces = CardFinder(traits=["MUTANT", "X-FACTOR", "X-FORCE", "X-MEN"]).Checks(faces)

        value = len(faces)
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mutant_slayers_revealed
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("MARAUDER", Minion),
            quickstrike=1,
        ),
    ]

