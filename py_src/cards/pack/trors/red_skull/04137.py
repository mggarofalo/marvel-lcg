from . import *

# Spreading Lies

def GetAbilities() -> Sequence['Ability']:

    def spreading_lies_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 2, effect)


    def spreading_lies_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spreading_lies_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            spreading_lies_boost
        ),
    ]

