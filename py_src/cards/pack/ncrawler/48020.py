from . import *

# * Astonishing X-Men

def GetAbilities() -> Sequence['Ability']:

    def astonishing_x_men_remove(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        this.RemoveThreatFromSchemes([this], 1, effect)

    def astonishing_x_men(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        units = Worlds.GetOnFieldEnemies(effect)
        Faces.GiveStatus(units, "Stunned", effect)
        Faces.GiveStatus(units, "Confused", effect)

    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            CardFinder2("X-MEN", Unit2),
            astonishing_x_men_remove,
            attacker=Enemy,
            take_no_damage=True,
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            astonishing_x_men,
        ),
    ]

