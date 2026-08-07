from . import *

# Titanium Exoskeleton

def GetAbilities() -> Sequence['Ability']:

    def titanium_exoskeleton(targets: Sequence['CardFace'], effect: 'Effect'):
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()

        return initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("3")
            ),
            AbilityFactory.ForChoiceAbility(
                "Remove a confused or stunned status card from attached enemy",
                lambda targets:
                    Faces.DiscardAll(targets, effect),
            ).SetTarget(names=["Confused", "Stunned"],
                check_fn=lambda effect, face:
                    this.GetBindFace() == face.GetBindFace(),
                canbe_discard=True),
        ) != []


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            fewest_remaining_hp=True
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "AttachedEnemy",
            cannot_take_more_than_damage=2,
            is_from_attack=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Custom(None, titanium_exoskeleton))
    ]

