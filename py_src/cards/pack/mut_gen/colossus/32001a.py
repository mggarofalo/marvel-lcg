from . import *

# * Colossus

def GetAbilities() -> Sequence['Ability']:

    def colossus(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.ThisCanHaveAdditionalTough(
            1
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            colossus,
            to_this_form=True,
        ).SetTarget("YourIdentity", canbe_tough=True)
        .SetName("Steel Skin")
    ]

