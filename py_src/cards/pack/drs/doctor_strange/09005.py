from . import *

# Master of the Mystic Arts

def GetAbilities() -> Sequence['Ability']:

    def master_of_the_mystic_arts(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ResolveSpecialAbility(effect.targets, effect)

        Faces.MoveAllToDeck(effect.targets, initiator.additional_deck, "Top", effect)
        Faces.FlipAllTo(effect.targets, True, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            master_of_the_mystic_arts,
            conditions=[InvocationTopCardHasTarget]
        ).SetPlay()
        .SetTarget(SelectInvocationTopCard)
        .SetCost(GetCostOfInvocationTopCard)
    ]

