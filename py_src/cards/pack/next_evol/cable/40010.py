from . import *

# Forced Amnesia

def GetAbilities() -> Sequence['Ability']:

    def forced_amnesia(effect: 'Effect', message: 'Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        targets = [this] + effect.targets
        Faces.MoveAllTo(targets, Worlds.VictoryDisplay(effect), effect)


    return [
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.HeroResponse,
            CardFinder(card_type=SchemeSide2, is_permanent=False),
            forced_amnesia,
        ).SetTarget("Trigger"),
    ]

