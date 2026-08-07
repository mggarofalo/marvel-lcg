from . import *

# Ready for Action

def GetAbilities() -> Sequence['Ability']:

    def ready_for_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ready_for_action
        ).SetPlay()
        .SetTarget("YourAlly", canbe_tough=True)
    ]

