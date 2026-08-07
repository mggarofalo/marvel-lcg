from . import *

# Neocarbon Scales

def GetAbilities() -> Sequence['Ability']:

    def neocarbon_scales(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)
            Faces.GiveFacedownBoostCards([villain], 1, effect)
            return True
        return False


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.ReduceCharacterTakeDamageBy(
            "AttachedVillain",
            1,
            this_attached_unit_only=True,
            from_each_attack=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Custom(None, neocarbon_scales)),
    ]

