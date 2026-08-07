from . import *

# Ice Slide

def GetAbilities() -> Sequence['Ability']:

    def ice_slide(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo([this], initiator.player_deck, effect)

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
            attack=1,
            defense=1,
            trait="AERIAL",
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            ice_slide,
            to_form=AlterEgo,
        ),
    ]

