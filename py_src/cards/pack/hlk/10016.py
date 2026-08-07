from . import *

# "You'll Pay for That!"

def GetAbilities() -> Sequence['Ability']:

    def you_will_pay_for_that(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = message.dealt_damage
        this.RemoveThreatFromSchemes(effect.targets, min(value, 5), effect)

    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.HeroResponse,
            Villain,
            "You",
            you_will_pay_for_that,
            conditions=[
                lambda effect, message:
                    message.to_player != None and \
                    message.attacked == message.to_player.GetIdentity()
            ],
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

