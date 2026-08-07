from . import *

# * Madame Hydra

def GetAbilities() -> Sequence['Ability']:

    def madame_hydra(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd|Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Legions of Hydra",
            card_type=SchemeSide2
        )

        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)


    return [
        AbilityFactory.ThisCannotTakeDamageWhile(
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="Legions of Hydra",
                        card_type=SchemeSide2
                    ) != None
            ]
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            "This",
            madame_hydra
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            madame_hydra
        )
    ]

