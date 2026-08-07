from . import *

# Not my Responsibility

def GetAbilities() -> Sequence['Ability']:

    def not_my_responsibility(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.SetBeInstead(effect)
        value = message.value
        effect.targets[0].CastTo(Hero|Ally|AlterEgo).TakeDamage(this, value, effect)


    return [
        AbilityFactory.WhenThreatWouldBePlacedOn(
            AbilityType.Interrupt,
            None,
            not_my_responsibility
        ).SetPlay().SetLabel()
        .SetTarget("YouControlUnit", finder=CardFinder(card_type=Identity|Ally)),
    ]

