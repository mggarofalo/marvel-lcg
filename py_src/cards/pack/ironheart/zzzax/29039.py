from . import *

# Air Static

def GetAbilities() -> Sequence['Ability']:

    def air_static(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        def action(player: 'Player'):
            if FacesCounter.GetPrintedResourcesCount(player.GetControlCards() + player.hand_cards.GetAll(), "Y") > 0:
                this.DealIndirectDamage(player.GetIdentity(), 2, effect)

        Players.ForEachPlayer(effect, action)

    def air_static_cost(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        this = effect.this.CastTo(Environment)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        return initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard a card you control with a printed [energy] resource",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(from_where=["YouControlCards"], has_printed_res="Y", canbe_discard=True),
            AbilityFactory.ForChoiceAbility(
                "Take 2 indirect damage",
                lambda targets:
                    identity.TakeIndirectDamage(this, 2, effect)
            )
        ) != []


    return [
        AbilityFactory.WhenPhaseBegin(
            AbilityType.ForcedInterrupt,
            "Villain",
            air_static
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Custom(None, air_static_cost)),
    ]

