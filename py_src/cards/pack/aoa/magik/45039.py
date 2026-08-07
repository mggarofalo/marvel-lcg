from . import *

# Soul Strike

def GetAbilities() -> Sequence['Ability']:

    def soul_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 4, effect)

        face = initiator.player_deck.GetTop()
        if face:
            res = FacesCounter.GetPrintedResources([face])
            if res.HasColor("R"):
                Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            soul_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

