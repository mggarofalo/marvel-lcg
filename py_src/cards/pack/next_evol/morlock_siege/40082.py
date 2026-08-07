from . import *

# Bolstered by Wrath

def GetAbilities() -> Sequence['Ability']:

    def bolstered_by_wrath_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        num = len(GetVillainsUnderRouted(effect))
        message.GainBoostIcon(num, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(lambda effect, faces: Cost("1") * len(GetVillainsUnderRouted(effect)))
        .SetCostFunc(CostFunc.Exhaust("YouControlUnit")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            bolstered_by_wrath_boost
        ),
    ]

