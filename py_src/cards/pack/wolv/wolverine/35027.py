from . import *

# Past Demons

def GetAbilities() -> Sequence['Ability']:

    def past_demons(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def discard(targets: Sequence['CardFace']):
            Faces.GiveStatus(targets, "Stunned", effect)
            Faces.GiveStatus(targets, "Confused", effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Logan → remove Past Demons from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "You are stunned and confused. Discard this card",
                discard,
            ).SetTarget("YourIdentity", canbe_status=["Stunned", "Confused"]),
        )

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            past_demons
        ),
    ]

