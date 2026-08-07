from . import *

# * James Rhodes

def GetAbilities() -> Sequence['Ability']:

    def james_rhodes(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

    def james_rhodes_change(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        Faces.RemoveCountersOn([identity], "All", 'ammo', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            james_rhodes
        ).SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"])
        .LimitOncePerPhase(),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            james_rhodes_change,
            to_this_form=True
        ),
    ]

