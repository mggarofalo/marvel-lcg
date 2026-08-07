from . import *

# Drop Kick

def GetAbilities() -> Sequence['Ability']:

    def drop_kick(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)

        if effect.GetPaidResources().OnlyHasColor("R"):
            Faces.GiveStatus(effect.targets, "Stunned", effect)
            initiator = effect.GetInitiator()
            initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            drop_kick
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

