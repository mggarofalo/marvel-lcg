from . import *

# Run and Gun

def GetAbilities() -> Sequence['Ability']:

    def run_and_gun(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            run_and_gun
        ).SetPlay().SetLabel()
        .SetTarget(
            CardFinder(name="Venom", card_type=Hero, canbe_ready=True)|
            CardFinder(trait="WEAPON", card_type=Upgrade, canbe_ready=True),
            range="All"
        ),
    ]

