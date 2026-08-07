from . import *

# Haywire

def GetAbilities() -> Sequence['Ability']:

    def haywire(effect: 'Effect', message: 'Message.WhenCountingResourcesOnCards') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetReturnValue(Resources("Y"), effect)

    def haywire_cost(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        return initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard a card you control with a printed [energy] resource",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(has_printed_res="Y", from_where=["YouControlCards"], canbe_discard=True),
            AbilityFactory.ForChoiceAbility(
                "Take 2 indirect damage",
                lambda targets:
                    identity.TakeIndirectDamage(this, 2, effect)
            )
        ) != []


    return [
        AbilityFactory.WhenCountingResourcesOnCards(
            None,
            haywire,
            from_where=DeckType.HandsArea,
        ),
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Custom(None, haywire_cost))
    ]

