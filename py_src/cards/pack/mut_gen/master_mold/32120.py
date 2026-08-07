from . import *

# Insert Virus Program

def GetAbilities() -> Sequence['Ability']:

    def insert_virus_program(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        enemies = Worlds.FindCardsOnField(
            effect,
            trait="SENTINEL",
            card_type=Enemy
        )

        this.DealDamage(enemies, 2, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            insert_virus_program,
        ),
    ]

