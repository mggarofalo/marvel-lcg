from . import *

# Prime Real Estate

def GetAbilities() -> Sequence['Ability']:

    def prime_real_estate(effect: 'Effect', message: 'Message.WhenCardWouldBePlacedCounter') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        message.SetBeInstead(effect)
        this.PlaceThreatOnSchemes([this], 1, effect)


    return [
        AbilityFactory.WhenCardWouldBePlacedCounter(
            AbilityType.ForcedInterrupt,
            CardFinder(trait="ENCHANTMENT"),
            'charm',
            prime_real_estate,
            conditions=[
                lambda effect, message:
                    message.trigger.card.area.GetOwnerPlayer().GetIdentity().HasTrait("ENTHRALLED")
            ]
        ),
    ]

