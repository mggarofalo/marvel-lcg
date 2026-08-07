from . import *

# Criminal Enterprise

def GetAbilities() -> Sequence['Ability']:

    def criminal_enterprise(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Norman Osborn",
            card_type=Villain
        )
        assert villain, f"{effect=}"
        villain.card.Flip(effect)
        this.card.Flip(effect, False)

    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            "2*",
            'infamy',
        ),
        AbilityFactory.IfThereIsNoCounterHere(
            'infamy',
            criminal_enterprise,
        )
    ]

