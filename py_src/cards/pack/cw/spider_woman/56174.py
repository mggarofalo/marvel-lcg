from . import *

# Finesse

def GetAbilities() -> Sequence['Ability']:

    def finesse(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'skill', effect)


    return [
        AbilityFactory.MaxCounterHere(
            3,
            'skill'
        ),
        AbilityFactory.WhenCardRevealed(
            AbilityType.ForcedInterrupt,
            Treachery,
            finesse
        ),
    ]

