from . import *

# * Hairbag

def GetAbilities() -> Sequence['Ability']:

    def hairbag_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action():
            Faces.ShuffleAllTo([this], "EncounterDeck", effect)

        message.AfterThisActivation(
            effect,
            action
        )


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hairbag_boost
        ),
    ]

