from . import *

# Ice Wall

def GetAbilities() -> Sequence['Ability']:

    def ice_wall(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        message.SetBeInstead(effect)

        damage = message.will_take_damage
        Faces.PlaceCountersOn([this], damage, 'damage', effect)

        if this.GetCounters('damage') >= 8:
            Faces.DiscardAll([this], effect)

            if message.would_atk_message:
                def action():
                    face = GetSetAsideFrostbite(initiator)
                    if face and message.attacker:
                        face.AttachTo2(message.attacker, effect)

                RunAt.AfterEnemyActivationEnd(
                    effect,
                    message.would_atk_message,
                    action
                )

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            Identity,
            ice_wall,
            is_from_attack=True,
            who_deal_damage=Enemy
        ),
    ]

