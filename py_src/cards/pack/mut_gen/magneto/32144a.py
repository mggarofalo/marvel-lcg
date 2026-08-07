from . import *

# Boarding Party

def GetAbilities() -> Sequence['Ability']:

    def boarding_party(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.card.Flip(effect)

        # RevealCards(CardFinder(name="Sabotage Master Mold"), effect)


    return [
        AbilityFactory.UnitCannotHaveMoreThanSustainedDamage(
            CardFinder(name="Magneto"),
            more_than_sustained_damage="6*"
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            boarding_party,
        ),
    ]

