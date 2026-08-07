from . import *

# Pain Tolerance

def GetAbilities() -> Sequence['Ability']:

    def pain_tolerance(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder(card_class="IdentitySpecific"),
            pain_tolerance
        ).SetTarget("YourIdentity", canbe_heal=True),
    ]

