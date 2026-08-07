from . import *

# * Ant-Man: Scott Lang

def GetAbilities() -> Sequence['Ability']:

    def is_gaint_trait(effect: 'Effect', ui: List['CardFace']) -> int:
        initiator = effect.GetInitiator()
        if initiator.IsInHeroForm("GIANT"):
            ui.append(initiator.GetHero())
            return 1
        return 0

    def is_tiny_trait(effect: 'Effect', ui: List['CardFace']) -> int:
        initiator = effect.GetInitiator()
        if initiator.IsInHeroForm("TINY"):
            ui.append(initiator.GetHero())
            return 1
        return 0

    return [
        AbilityFactory.ThisGainKeyword(
            is_gaint_trait,
            trait="GIANT",
            attack=1,
            change_on_event=OnEvent.Trait("YourIdentity")
        ),
        AbilityFactory.ThisGainKeyword(
            is_tiny_trait,
            trait="TINY",
            thwart=1,
            change_on_event=OnEvent.Trait("YourIdentity")
        )
    ]

