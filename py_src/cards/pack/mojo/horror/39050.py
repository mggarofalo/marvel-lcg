from . import *

# * The Kraken

def GetAbilities() -> Sequence['Ability']:

    def the_kraken(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        for face in Worlds.GetOnFieldCharacters(effect):
            if face != this:
                face.TakeDamage(this, 1, effect)

    def the_kraken_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.GetOnFieldFriendlyCharacters(effect)
        this.HealthUnits(faces, 1, effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            the_kraken,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_kraken_defeated
        ),
    ]

