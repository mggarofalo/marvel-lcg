from . import *

# Delusion of Collusion

def GetAbilities() -> Sequence['Ability']:

    def delusion_of_collusion_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        boost_for_message = message.would_atk_message
        defender = boost_for_message.GetDefender()
        if defender and Ally.IsType(defender):
            ally = defender.CastTo(Ally)
            damage = FacesCounter.GetPrintedCost([ally])

            def action():
                player.GetIdentity().TakeIndirectDamage(this, damage, effect)

            ally.effect.RegisterTemp(
                AbilityFactory.WhenUnitBeDefeated(
                    AbilityType.Temp0,
                    "This",
                    lambda damage_effect, damage_message:
                        action(),
                    conditions=[
                        lambda damage_effect, damage_message:
                            damage_message.would_atk_message == boost_for_message,
                    ]
                ),
                unregister_after_exec=True,
                until_turn_end=True
            )

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        *AbilityFactory.PlayersCannotReadyCardWhile(
            "You",
            CardFinder(card_type=Ally),
            control_by="You"
        ),
        *AbilityFactory.PlayersCannotReadyCardWhile(
            "You",
            CardFinder2("PERSONA", Support),
            control_by="You"
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(check_effect=lambda effect, face:
                CardFinder(card_type=Ally).Check(face) or \
                CardFinder2("PERSONA", Support).Check(face),
            from_where=["YouControlCards"]
        )),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            delusion_of_collusion_boost,
            during_attack=True,
            is_undefended_attack=False,
        ),
    ]

