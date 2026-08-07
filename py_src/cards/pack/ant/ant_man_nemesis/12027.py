from . import *

# * Yellowjacket

def GetAbilities() -> Sequence['Ability']:

    def is_giant_trait(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        player = this.GetEngagedPlayer()
        if player.IsInHeroForm("GIANT"):
            ui.append(player.GetHero())
            return 1
        return 0

    def is_tiny_trait(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        player = this.GetEngagedPlayer()
        if player.IsInHeroForm("TINY"):
            ui.append(player.GetHero())
            return 1
        return 0

    return [
        AbilityFactory.ThisGainKeyword(
            is_giant_trait,
            trait="GIANT",
            retaliate=1,
            change_on_event=OnEvent.Trait("YourIdentity")
        ),
        AbilityFactory.ThisGainKeyword(
            is_tiny_trait,
            trait="TINY",
            attack=1,
            change_on_event=OnEvent.Trait("YourIdentity")
        )
    ]

