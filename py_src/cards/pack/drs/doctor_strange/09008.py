from . import *

# * Sanctum Sanctorum

def GetAbilities() -> Sequence['Ability']:

    def sanctum_sanctorum(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)
        initiator.DrawUp(1, effect)

    # def has_spell_in_discard(effect: 'Effect') -> bool:
    #     initiator = effect.GetInitiator()
    #     return initiator.discard_pile.FindCard(trait="SPELL") != None

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            sanctum_sanctorum,
            # condition=lambda effect, message:
            #     has_spell_in_discard(effect),
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(trait="SPELL", from_where=["YourDiscardPile"])
        .SetHasNoTargetEffect()
    ]

