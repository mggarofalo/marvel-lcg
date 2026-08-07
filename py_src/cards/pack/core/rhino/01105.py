from . import *

# I'm Tough

def GetAbilities() -> Sequence['Ability']:

    def i_am_tough(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Rhino",
            card_type=Villain
        )

        if villain and not villain.IsTough():
            Faces.GiveStatus([villain], "Tough", effect)
        else:
            this.GainSurge(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            i_am_tough
        ).SetTarget(Villain)
    ]
