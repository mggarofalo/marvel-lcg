from . import *

# Exorcism

def GetAbilities() -> Sequence['Ability']:

    def exorcism(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 4, effect)
        face = initiator.player_deck.GetTop()
        if face:
            res = FacesCounter.GetPrintedResources([face])
            if res.HasColor("B"):
                Faces.GiveStatus(effect.targets2, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            exorcism
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Villain, canbe_confused=True),
    ]

