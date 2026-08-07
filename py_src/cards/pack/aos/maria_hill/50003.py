from . import *

# All-Points Bulletin

def GetAbilities() -> Sequence['Ability']:

    def all_points_bulletin(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        size = len(initiator.GetControlCardsByType(CardFinder2("S.H.I.E.L.D", Support), support=True))

        for _ in range(size):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Remove 1 threat from a scheme",
                    lambda targets:
                        this.RemoveThreatFromSchemes(targets, 1, effect)
                ).SetTarget(Scheme2),
                AbilityFactory.ForChoiceAbility(
                    "Deal 1 damage to an enemy",
                    lambda targets:
                        this.DealDamage(targets, 1, effect)
                ).SetTarget(Enemy)
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            all_points_bulletin
        ).SetPlay().SetLabel(),
    ]

