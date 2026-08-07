from . import *

# Elite Minion

def GetAbilities() -> Sequence['Ability']:

    def elite_minion(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = [this] + CardFinder(name="Reluctant Foe").Checks(this.GetAttachedAttachments())
        Faces.RemoveAllFromGame(faces, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            elite_minion
        ),
    ]

