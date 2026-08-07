from . import *

# Southern Cross

def GetAbilities() -> Sequence['Ability']:

    def southern_cross(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        value = 6
        if identity.HasTrait("AERIAL"):
            value += 2

        this.DealDamage(effect.targets, value, effect)

        if identity.retaliate > 0:
            Faces.GiveStatus(effect.targets, "Stunned", effect)
        if identity.IsStalwart():
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            southern_cross
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

