from . import *

# Casket of Ancient Winters

def GetAbilities() -> Sequence['Ability']:

    def casket_of_ancient_winters(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        face = GetInfinityStone(effect).GetTopCard()
        if face:
            face.Reveal(None, effect)

        SwapLokiWithRandomSetAsideLokiVillain(effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            casket_of_ancient_winters,
        ),
    ]

