from . import *

# Orbital Decay

def GetAbilities() -> Sequence['Ability']:

    def orbital_decay(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.card.Flip(effect)

        # RevealCards(CardFinder(name="Physical Strain"), effect)

    return [
        AbilityFactory.UnitCannotHaveMoreThanSustainedDamage(
            CardFinder(name="Magneto"),
            more_than_sustained_damage="18*"
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            orbital_decay,
        ),
    ]

