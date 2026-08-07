from . import *

# Grapple

def GetAbilities() -> Sequence['Ability']:

    def grapple(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        Faces.GiveStatus([identity], "Stunned", effect)

        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            grapple
        ).SetPlay().SetLabel()
        .SetTarget(Enemy)
        .SetHasNoTargetEffect(),
    ]

