from . import *

# * Mockingbird: Bobbi Morse

def GetAbilities() -> Sequence['Ability']:

    def mockingbird(effect: 'Effect', message: 'Message2'):
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            mockingbird
        ).SetTarget(Enemy, canbe_stunned=True)
    ]
