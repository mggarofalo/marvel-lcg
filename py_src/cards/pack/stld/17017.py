from . import *

# Target Practice

def GetAbilities() -> Sequence['Ability']:

    def target_practice(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        face = message.trigger.CastTo(Ally)
        face.GainForThisActive(effect, message, attack=2)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            CardFinder(card_type=Ally, with_attach=CardFinder2("WEAPON", Upgrade)),
            target_practice,
        ).SetCostFunc(CostFunc.Discard("This"))
    ]

