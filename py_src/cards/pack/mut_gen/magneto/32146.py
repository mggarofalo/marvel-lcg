from . import *

# M-Type Sentinel

def GetAbilities() -> Sequence['Ability']:

    def m_type_sentinel(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        magneto = Worlds.FindCardOnField(
            effect,
            name="Magneto",
        )
        if magneto:
            Faces.GiveStatus([magneto], "Tough", effect)


    def m_type_sentinel_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        magneto = Worlds.FindCardOnField(
            effect,
            name="Magneto",
            card_type=Enemy
        )
        if magneto:
            Faces.GiveStatus([magneto], "Tough", effect)
            Faces.GiveFacedownBoostCards([magneto], 1, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            m_type_sentinel
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            m_type_sentinel_boost
        ),
    ]

