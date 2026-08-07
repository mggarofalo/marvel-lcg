from . import *

# Skrull Business

def GetAbilities() -> Sequence['Ability']:

    def skrull_business(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceCardHere(message.trigger, False, effect)


    return [
        AbilityFactory.AfterCardDiscard(
            AbilityType.ForcedResponse,
            None,
            skrull_business,
            from_where="PlayerDeckTop"
        ),
    ]

