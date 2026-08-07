from . import *

# Family Emergency

def GetAbilities() -> Sequence['Ability']:

    def family_emergency(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        identity = player.GetIdentity()

        def you_are_stunned(targets: Sequence['CardFace']):
            Faces.GiveStatus([identity], "Stunned", effect)
            this.GainSurge(1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Carol Danvers → remove Family Emergency from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=identity.IsName("Carol Danvers"),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "You are stunned. This card gains surge. Discard this obligation",
                you_are_stunned
            ).SetTarget("YourIdentity")
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            family_emergency
        )
    ]

