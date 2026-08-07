from . import *

# X-Men Instruction

def GetAbilities() -> Sequence['Ability']:

    def x_men_instruction(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

        if Worlds.FindCardOnField(
            effect,
            name="X-Mansion"
        ):
            initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            x_men_instruction,
        ).SetPlay(only_if_your_identity_has_trait="MUTANT").SetLabel()
        .SetTarget(Ally, trait="X-MEN", range=(lambda effect: 0 if XMansionIsInPlay(effect) else 1, 2), from_where=["YourDiscardPile"]),
    ]

