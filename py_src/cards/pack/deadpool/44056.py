from . import *

# * Rock, Paper, Scissors

def GetAbilities() -> Sequence['Ability']:

    def rock_paper_scissors(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        hand_face = effect.targets[0]
        deck_top_face = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards[0]

        def is_beat(a: Resources, b: Resources) -> bool:
            if a.val == 0 or b.val == 0:
                return False
            if a.g > 0:
                if b.g <= 0:
                    return True
            elif a.r > 0:
                if b.y > 0:
                    return True
            elif a.b > 0:
                if b.r > 0:
                    return True
            elif a.y > 0:
                if b.b > 0:
                    return True
            return False

        res1 = FacesCounter.GetPrintedResources([hand_face])
        res2 = FacesCounter.GetPrintedResources([deck_top_face])
        if is_beat(res1, res2):
            initiator.GainCard(deck_top_face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            rock_paper_scissors
        ).SetTarget(from_where=["YourHandCards"])
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourPlayerDeckTop")),
    ]

