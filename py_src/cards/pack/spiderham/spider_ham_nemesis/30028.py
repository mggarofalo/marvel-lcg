from . import *

# "Feast on This!"

def GetAbilities() -> Sequence['Ability']:

    def feast_on_this_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)
        for target in effect.targets:
            if target.CastTo(Unit2).IsConfused():
                ThisCardGainSurge(effect)
            else:
                Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            feast_on_this_revealed
        ).SetTarget("YourIdentity"),
    ]

