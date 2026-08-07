from . import *

# * Mirage: Dani Moonstar

def GetAbilities() -> Sequence['Ability']:

    def mirage(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)

    def check(effect: 'Effect', minion: 'CardFace') -> bool:
        this = effect.this.CastTo(Ally)
        Unused(this)

        if not Enemy.IsType(minion):
            return True

        return minion.scheme < this.thwart

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            mirage
        ).SetTarget(Enemy, canbe_stunned=True, check_fn=check),
    ]

