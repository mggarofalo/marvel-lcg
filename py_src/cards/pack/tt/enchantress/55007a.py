from . import *

# Hypnotic Gaze

def GetAbilities() -> Sequence['Ability']:

    def hypnotic_gaze(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.RemoveCountersOn([this], "All", 'charm', effect)
        this.card.Flip(effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="DEFIANT"
        ),
        AbilityFactory.IfThereAreAtLeastCounterHere(
            5,
            'charm',
            hypnotic_gaze
        ),
    ]

