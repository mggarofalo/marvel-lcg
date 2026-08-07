from . import *

# Acquire Infinity Formula

def GetAbilities() -> Sequence['Ability']:

    def acquire_infinity_formula(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Orion")
        if face:
            Faces.GiveStatus([face], "Tough", effect)
        else:
            player = Worlds.FindPlayerByName("Nick Fury", effect)
            if player:
                Find.FindAndPutIntoPlay(
                    effect,
                    player,
                    name="Orion",
                )

    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            CardFinder(name="Nick Fury"),
            acquire_infinity_formula
        ),
    ]

