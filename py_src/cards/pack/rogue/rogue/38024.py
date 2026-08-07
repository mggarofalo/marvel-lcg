from . import *

# Deadly Touch

def GetAbilities() -> Sequence['Ability']:

    def deadly_touch(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        friend = IfTouchedIsAttachTo(effect, Friend)
        if friend:
            this.DealDamage([friend], 2, effect)
        else:
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            deadly_touch
        ),
    ]

