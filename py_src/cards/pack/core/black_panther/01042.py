from . import *

# Ancestral Knowledge

def GetAbilities() -> Sequence['Ability']:

    def ancestral_knowledge(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

    return [
        # A: Can Ancestral Knowledge shuffle different versions of Wakanda Forever into Black Panther’s deck?
        # Q: No. Cards with the same title are considered to be the same card for the purpose of card abilities.
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            ancestral_knowledge
        ).SetPlay().SetTarget(
            range=(1, 3),
            from_where=["YourDiscardPile"],
            select_rule="DifferentCards",)
    ]

