from . import *

# Memories of Another Life

def GetAbilities() -> Sequence['Ability']:

    def memories_of_another_life(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            identity = player.GetIdentity()
            if identity.IsStunned():
                this.GainSurge(1, effect)
            else:
                Faces.GiveStatus([identity], "Stunned", effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust your alter-ego → remove Memories of Another Life from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "You are stunned",
                action
            ).SetTarget("YourIdentity")
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            memories_of_another_life
        )
    ]

