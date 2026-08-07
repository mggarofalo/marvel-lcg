from . import *

# * Spider-UK: Billy Braddock

def GetAbilities() -> Sequence['Ability']:

    def spider_uk(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = len(initiator.GetControlCards2(CardFinder2("WEB-WARRIOR")))
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.Interrupt,
            "This",
            spider_uk,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().IsControl(CardFinder2("WEB-WARRIOR"))
            ]
        ).SetTarget("Attacker"),
    ]

