from . import *

# Berserker Barrage

def GetAbilities() -> Sequence['Ability']:

    def berserker_barrage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def repeat_this_ability():
            # effect.Repeat(name="take 2 damage to repeat this ability", ex_cost_func=CostFunc.TakeDamage(Select(None, CardFinder(card_type=Identity)), 2)))
            # effect.Repeat(message, forced=False)

            def repeat_this_ability_internal(targets: Sequence['CardFace']):
                initiator.GetIdentity().TakeDamage(this, 2, effect)
                action(targets)

            initiator.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    repeat_this_ability_internal,
                ).SetLabel('attack')
                .SetTarget(Enemy)
            )

        def action(targets: Sequence['CardFace']):
            this.effect.RegisterTemp(
                AbilityFactory.AfterUnitDefeatedUnitInternal(
                    AbilityType.Temp0,
                    None,
                    targets,
                    lambda defeat_effect, defeat_message:
                        repeat_this_ability(),
                    conditions=[
                        lambda defeat_effect, defeat_message:
                            defeat_message.would_atk_message != None and \
                            defeat_message.would_atk_message.by_effect == effect and \
                            defeat_message.target in targets,
                    ]
                ),
                unregister_after_exec=True,
                until_turn_end=True,
                until_resolve_effect=effect,
            )
            this.DealDamage(targets, 4, effect)

        action(effect.targets)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            berserker_barrage
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

