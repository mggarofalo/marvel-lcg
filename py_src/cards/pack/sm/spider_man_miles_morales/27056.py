from . import *

# Keeping Secrets

def GetAbilities() -> Sequence['Ability']:

    def keeping_secrets(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            faces = Faces.DiscardAll(targets, effect)
            if len(faces) == 0:
                ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Miles Morales → remove Keeping Secrets from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard Ganke Lee and Jefferson Davis from play",
                action
            ).SetTarget(names=["Ganke Lee", "Jefferson Davis"], range=("All"), canbe_discard=True),
            AbilityFactory.Otherwise(
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            keeping_secrets
        ),
    ]

