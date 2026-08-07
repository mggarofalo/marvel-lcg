from . import *

# Leading Blow

def GetAbilities() -> Sequence['Ability']:

    def leading_blow(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = effect.cost_func.Get(CostFunc.Discard).return_discarded_cards
        value = -1 * FacesCounter.CountTotalBoostIcons(faces)
        message.GainATKForThisAttack(value, effect)

        def ready_your_hero(deal_effect: 'Effect', message: 'Message.AfterUnitAttackUnit'):
            if message.dealt_damage > 0:
                Faces.ReadyAll([initiator.GetHero()], effect)

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitAttackUnitInternal(
                AbilityType.Temp0,
                Hero,
                None,
                ready_your_hero,
                conditions=[
                    lambda atk_effect, atk_message:
                        message == atk_message.would_atk_message
                ],
            ),
            unregister_after_exec=True,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            leading_blow,
            is_basic_attack=True,
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Discard("EncounterDeckTop"))
    ]

