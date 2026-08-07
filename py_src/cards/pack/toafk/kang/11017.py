from . import *

# Macrobots

def GetAbilities() -> Sequence['Ability']:

    def macrobots(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        kang = Worlds.FindCardOnField(
            effect,
            name="Kang",
            card_type=Villain
        )
        assert kang
        Faces.GiveStatus([kang], "Tough", effect)

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            macrobots
        )
    ]

