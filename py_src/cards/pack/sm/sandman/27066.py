from . import *

# Sand Form

def GetAbilities() -> Sequence['Ability']:

    def sand_form(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        ResolveSurgingSandsOnCityStreets(effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Sandman"),
        ),
        AbilityFactory.WhenFaceWouldDealDamage(
            AbilityType.ForcedInterrupt,
            "You",
            sand_form,
            who_take_damage=CardFinder(name="Sandman"),
        ).SetCostFunc(CostFunc.Discard("This")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        )
    ]

