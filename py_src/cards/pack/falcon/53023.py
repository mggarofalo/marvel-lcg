from . import *

# * Captain America

def GetAbilities() -> Sequence['Ability']:

    def captain_america(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Find.Find(
            effect,
            name="Captain America's Shield",
        )
        if face:
            should_deal_damage = False
            if face.IsInPlay():
                should_deal_damage = True
            Faces.AddToHand([face], initiator, effect)

            if should_deal_damage:
                initiator.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "",
                        lambda targets:
                            this.DealDamage(targets, 4, effect)
                    ).SetTarget(Enemy)
                )

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_you_are_the_player=["Bucky Barnes", "Sam Wilson"]),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            captain_america
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("R")),
    ]

