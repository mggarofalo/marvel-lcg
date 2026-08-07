from . import *

# * Scarlet Spider: Ben Reilly

def GetAbilities() -> Sequence['Ability']:

    def scarlet_spider(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.SetBeInstead(effect)

        this.TakeDamage(message.source, message.will_take_damage, effect, message.would_deal_damage_message.would_attack_unit_message)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            CardFinder2("WEB-WARRIOR", Unit2),
            scarlet_spider
        ).SetTarget("Trigger", exclude="This"),
    ]

