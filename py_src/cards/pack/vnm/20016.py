from . import *

# * Star-Lord: Peter Quill

def GetAbilities() -> Sequence['Ability']:

    def star_lord(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DealEncounterCards(1, effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            ranged=True
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            star_lord
        ),
    ]

