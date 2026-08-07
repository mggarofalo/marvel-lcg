from . import *

# Scarlet Witch's Crest

def GetAbilities() -> Sequence['Ability']:

    def scarlet_witchs_crest(effect: 'Effect', message: 'Message.WhenBoostIconsWouldBeCount') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        command = initiator.AskChooseOneText(["increase", "decrease"])
        if command == "increase":
            num = 1
        else:
            num = -1
        message.UpdateBoostIcon(num, effect)


    return [
        AbilityFactory.WhenBoostIconsWouldBeCount(
            AbilityType.Interrupt,
            scarlet_witchs_crest
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

