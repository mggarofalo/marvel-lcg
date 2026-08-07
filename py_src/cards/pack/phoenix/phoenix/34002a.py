from . import *

# Phoenix Force

def GetAbilities() -> Sequence['Ability']:

    def phoenix_force(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.card.Flip(effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="RESTRAINED",
        ),
        AbilityFactory.AfterCardRemovedCounter(
            AbilityType.ForcedResponse,
            "This",
            'power',
            phoenix_force,
            is_last_counter=True,
        ),
    ]

