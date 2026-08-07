from . import *

# State of Madness

def GetAbilities() -> Sequence['Ability']:

    def state_of_madness(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        assert villain, f"{effect=}"
        villain.card.Flip(effect)
        this.card.Flip(effect, False)

    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            "2*",
            'madness',
        ),
        AbilityFactory.IfThereIsNoCounterHere(
            'madness',
            state_of_madness,
        )
    ]

