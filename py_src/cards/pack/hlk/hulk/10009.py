from . import *

# * Boundless Rage

def GetAbilities() -> Sequence['Ability']:

    def boundless_rage(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(hero_form_only=True),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hulk"),
            attack=1,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            boundless_rage
        )
    ]

