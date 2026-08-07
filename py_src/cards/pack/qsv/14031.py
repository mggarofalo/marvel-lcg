from . import *

# United We Stand

def GetAbilities() -> Sequence['Ability']:

    def united_we_stand(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            united_we_stand,
        ).SetPlay(only_if_your_identity_has_trait="AVENGER")
        .SetTarget(Friend, canbe_heal=True,
            range=(1, lambda effect: min(3, Worlds.GetVillainStage(effect))))
    ]

