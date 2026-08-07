from . import *

# * Martyr: Phyla-Vell

def GetAbilities() -> Sequence['Ability']:

    def martyr(effect: 'Effect', message: 'Message.AfterAllyTakeConsequentialDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)

    return [
        # If Martyr was Tough when defeating an enemy, the consequential damage dealt to her would be prevented by Tough, so her Response would not trigger. (She did not “take” consequential damage.)
        AbilityFactory.AfterAllyTakeConsequentialDamage(
            AbilityType.Response,
            "This",
            martyr,
            from_performing="Attack",
            defeated_face=Enemy,
        )
    ]

