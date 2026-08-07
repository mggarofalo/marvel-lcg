from . import *

# In Too Deep

def GetAbilities() -> Sequence['Ability']:

    def in_too_deep(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            res = Worlds.Enemies.EngagedMinionActivateAgainstYou(effect, player)
            if res.activated_cnt == 0:
                this.GainSurge(+1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Greer Nelson → remove In Too Deep from the game.",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Each minion engaged with you activates against you.",
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            in_too_deep
        ),
    ]

