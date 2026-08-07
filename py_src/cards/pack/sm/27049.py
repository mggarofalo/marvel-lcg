from . import *

# * Spider-Man: Peter Parker

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.Response,
            "This",
            spider_man
        ).SetTarget(Unit2, trait="WEB-WARRIOR", canbe_ready=True, another=True),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            spider_man
        ).SetTarget(Unit2, trait="WEB-WARRIOR", canbe_ready=True, another=True)
    ]

