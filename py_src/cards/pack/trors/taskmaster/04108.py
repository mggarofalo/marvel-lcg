from . import *

# Taskmaster's Training Camp

def GetAbilities() -> Sequence['Ability']:

    def taskmasters_training_camp(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        Faces.GiveStatus([message.trigger], "Tough", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            Minion,
            taskmasters_training_camp
        ),
    ]

