from . import *

# * Spider-Man 2099: Miguel O'Hara

def GetAbilities() -> Sequence['Ability']:

    def spider_man_2099(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReturnToHand(effect.targets, "Owner", effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            spider_man_2099
        ).SetTarget(Ally, trait="WEB-WARRIOR"),
    ]

