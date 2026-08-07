from . import *

# * Callisto

def GetAbilities() -> Sequence['Ability']:

    def callisto_revealed(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.ForcedInterrupt,
            "AnyPlayer",
            CardFinder(name="Knife Fight", card_type=Treachery),
            None,
            callisto_revealed,
        ),
    ]

