from . import *

# Sabotage Master Mold

def GetAbilities() -> Sequence['Ability']:

    def sabotage_master_mold(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        scheme = Worlds.AsideDeck(effect).FindCard(name="Orbital Decay", card_type=SchemeSide2)
        if scheme:
            scheme.Reveal(None, effect)

        Faces.AddToVictoryDisplay([this], effect)

    return [
        AbilityFactory.UnitCannotHaveMoreThanSustainedDamage(
            CardFinder(name="Magneto"),
            more_than_sustained_damage="12*"
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            sabotage_master_mold,
        ),
    ]

