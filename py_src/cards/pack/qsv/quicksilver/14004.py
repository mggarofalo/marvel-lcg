from . import *

# Double Time

def GetAbilities() -> Sequence['Ability']:

    def double_time(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def action(targets: Sequence[CardFace]):
            for target in targets:
                if Enemy.IsType(target):
                    this.DealDamage([target], 2, effect)
                elif Scheme2.IsType(target):
                    this.RemoveThreatFromSchemes([target], 2, effect)
                else:
                    assert False, f"{target=}"

        action(effect.targets)


        """
        You can then resolve your chosen options in either order,
        treating them as “separate sentences” of a single ability.
        If you resolve one sentence and then a new valid target appears for
        the next sentence, you can use the new target for that sentence.
        """

        player = effect.GetInitiator()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ).SetTarget(
                CardFinder(card_type=Enemy)|
                CardFinder(card_type=Scheme2, has_threat=True),
            )
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            double_time
        ).SetPlay()
        .SetTarget(
            CardFinder(card_type=Enemy)|
            CardFinder(card_type=Scheme2, has_threat=True),
        )
    ]

