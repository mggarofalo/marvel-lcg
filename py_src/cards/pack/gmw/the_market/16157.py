from . import *

# Wing It

def GetAbilities() -> Sequence['Ability']:

    def wing_it(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)

        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wing_it
        ).SetPlay().SetLabel()
        .SetTarget(Enemy)
        .SetHasNoTargetEffect(),
    ]

