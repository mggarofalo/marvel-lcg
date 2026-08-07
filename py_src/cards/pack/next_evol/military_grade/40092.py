from . import *

# Inhibitor Collar

def GetAbilities() -> Sequence['Ability']:

    def inhibitor_collar(targets: Sequence['CardFace'], effect: 'Effect'):
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()

        return initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust a character you control",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YouControlUnit", canbe_exhaust=True),
            AbilityFactory.ForChoiceAbility(
                "Take 3 damage",
                lambda targets:
                    initiator.GetIdentity().TakeDamage(this, 3, effect),
            ).SetTarget("YourIdentity"),
        ) != []


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            treat_as_blank=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.Action,
        ).SetCostFunc(CostFunc.Custom(None, inhibitor_collar))
        .AnyPlayerCanDoThis(),
    ]

