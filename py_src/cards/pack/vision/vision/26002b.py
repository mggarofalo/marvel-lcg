from . import *

# Intangible

def GetAbilities() -> Sequence['Ability']:

    def intangible(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=+2,
            defense=+2,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            intangible,
            to_this_additional_form=True,
        ),
    ]

