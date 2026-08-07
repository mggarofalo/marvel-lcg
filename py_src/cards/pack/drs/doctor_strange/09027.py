from . import *

# Physical Toll

def GetAbilities() -> Sequence['Ability']:

    def physical_toll(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def play_costs_3_additional_resources(targets: Sequence['CardFace']):

            pay_effects = this.effect.RegisterTemp(
                AbilityFactory.IncreaseResourceCostToPlay(
                    Event,
                    +3,
                    player,
                ),
                unregister_after_exec=False
            )
            def discard_this(effect: 'Effect', message: 'Message2'):
                Faces.DiscardAll([this], effect)
                Effects.UnRegister(pay_effects)

            this.effect.RegisterTemp(
                AbilityFactory.AfterPlayerPlayedCard(
                    AbilityType.Temp0,
                    player,
                    Event,
                    discard_this,
                ),
                unregister_after_exec=True,
            )

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Stephen Strange → remove Physical Toll from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "The next event you play costs 3 additional resources",
                play_costs_3_additional_resources
            )
        )

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            physical_toll
        )
    ]

