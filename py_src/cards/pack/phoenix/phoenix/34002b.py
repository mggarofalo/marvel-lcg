from . import *

# Phoenix Force

def GetAbilities() -> Sequence['Ability']:

    def phoenix_force(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.card.Flip(effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="UNLEASHED",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Phoenix"),
            thwart=-2,
            attack=+2,
        ),
        AbilityFactory.AfterCounterPlacedOn(
            AbilityType.ForcedInterrupt,
            "This",
            'power',
            phoenix_force,
            at_least=4
        ),
    ]

