from . import *

# Chill Out!

def GetAbilities() -> Sequence['Ability']:

    def chill_out(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

        face = GetSetAsideFrostbite(initiator)

        if face:
            face.AttachTo2(effect.targets2[0], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            chill_out
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Enemy),
    ]

