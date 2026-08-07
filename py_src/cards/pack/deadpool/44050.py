from . import *

# * Plot Convenience

def GetAbilities() -> Sequence['Ability']:

    def plot_convenience(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        for target in effect.targets:
            if target.card.IsInHand():
                this.PlaceCardHere(target, False, effect)
            elif target.card.area == this.GetPlacedCardArea():
                initiator.GainCard(target, effect)

    def plot_convenience_select(effect: 'Effect') -> Sequence[CardFace]:
        faces = effect.this.GetPlacedCardArea().Get()
        if len(faces) >= 3:
            return faces
        faces += CardFinder(card_class="Aspect").Checks(effect.GetInitiator().hand_cards.Get())
        return faces

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            plot_convenience
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(plot_convenience_select)
        .AnyPlayerCanDoThis(),
    ]

