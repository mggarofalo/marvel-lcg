from . import *

# Badoon Sentry

def GetAbilities() -> Sequence['Ability']:

    def badoon_sentry_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            if villain.IsTough():
                message.GainBoostIcon(2, effect)
            else:
                Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            badoon_sentry_boost
        ),
    ]

