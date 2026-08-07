from . import *

# Concussive Bombs

def GetAbilities() -> Sequence['Ability']:

    def concussive_bombs(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget(Upgrade, from_where=["YouControlCards"], canbe_exhaust=True)
        )
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget(Support, from_where=["YouControlCards"], canbe_exhaust=True)
        )

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            Villain,
            "You",
            concussive_bombs
        ).SetCostFunc(CostFunc.Counter("This", 1, 'bomb'))
    ]

