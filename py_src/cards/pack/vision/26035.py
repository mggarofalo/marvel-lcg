from . import *

# Joining Forces

def GetAbilities() -> Sequence['Ability']:

    def joining_forces(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        for face in effect.targets:
            player = face.GetControlByPlayer()
            face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            joining_forces
        ).SetPlay().SetLabel()
        .SetTarget(Ally, traits=["AVENGER", "GUARDIAN"],
            range=(1, 2),
            select_rule="MustIncludeTraits",
            from_where=["AnyHandCards"]
        ),
    ]

