from . import *

# * Pete Wisdom

def GetAbilities() -> Sequence['Ability']:

    def pete_wisdom(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="X-FORCE"),
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            Treachery,
            pete_wisdom,
            which_abilities=["WhenRevealed"],
        ).SetTarget("This", canbe_heal=True),
    ]

