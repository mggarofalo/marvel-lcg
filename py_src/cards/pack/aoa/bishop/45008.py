from . import *

# Command Authority

def GetAbilities() -> Sequence['Ability']:

    def command_authority(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)
        if effect.IsPaidForWithCardType(Resource):
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            command_authority
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

