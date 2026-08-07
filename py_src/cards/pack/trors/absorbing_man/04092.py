from . import *

# Super Absorbing Power

def GetAbilities() -> Sequence['Ability']:

    def super_absorbing_power_env(effect: 'Effect', unit: 'CardFace', diff: int) -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        villain = unit.CastTo(Villain)
        villain.GainTraits(diff, ["ICE", "METAL", "STONE", "WOOD"], effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Absorbing Man"),
            apply=super_absorbing_power_env,
        ),
    ]

