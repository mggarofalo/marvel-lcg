from . import *

# No Longer Worthy

def GetAbilities() -> Sequence['Ability']:

    def no_longer_worthy(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Worlds.SetGameOver(True, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Apocalypse"),
            when_attach_operation=lambda face, effect:
                effect.this.HealthUnits([face], "5*", effect),
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Apocalypse"),
            while_face_is_in_play=CardFinder2("PRELATE", Minion)
        ),
        AbilityFactory.IgnoreAbilityOnCard(
            MainScheme,
            "ForcedInterrupt",
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Apocalypse"),
            no_longer_worthy
        ),
    ]

