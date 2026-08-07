from . import *

# Two for One

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Challenge,
            Villain,
            lambda effect, message:
                message.SetAtLeastHealth(1, effect),
            conditions=[
                lambda effect, message:
                    message.trigger.CastTo(Villain).IsFinalStage(effect) and \
                    ( \
                        not message.by_effect.this.IsName("Fastball Special") or \
                        not message.would_deal_damage_message.IsOverkill()
                    ),
            ]
        ),
    ]

