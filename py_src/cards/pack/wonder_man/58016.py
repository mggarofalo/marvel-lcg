from . import *

# Battlefield Benevolence

def GetAbilities() -> Sequence['Ability']:

    def battlefield_benevolence(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        target = effect.cost_func.Get(CostFunc.Heal).return_targets
        Faces.GiveStatus(target, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            battlefield_benevolence
        ).SetPlay().SetLabel()
        # .SetTarget(Enemy, canbe_confused=True)
        .SetCostFunc(CostFunc.Heal(2, Select.From(CardFinder(card_type=Enemy, canbe_confused=True)))),
    ]

