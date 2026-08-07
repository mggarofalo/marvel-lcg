from . import *

# Dive Bomb

def GetAbilities() -> Sequence['Ability']:

    def dive_bomb(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 7, effect)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetLabel('attack')
            .SetTarget(Enemy, range="All", exclude=effect.targets)
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            dive_bomb,
        ).SetPlay(only_if_your_identity_has_trait="AERIAL").SetLabel('attack')
        .SetTarget(Enemy)
    ]

