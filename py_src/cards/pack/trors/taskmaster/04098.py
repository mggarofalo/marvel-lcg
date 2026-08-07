from . import *

# * Shang-Chi

def GetAbilities() -> Sequence['Ability']:

    def shang_chi(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            shang_chi,
        ).SetCost(Cost("Y"))
        .SetTarget(Enemy, canbe_stunned=True)
    ]

