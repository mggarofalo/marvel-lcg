from . import *

# Solar Beam

def GetAbilities() -> Sequence['Ability']:

    def solar_beam_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 7, effect)

    def solar_beam_thwart(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            solar_beam_attack,
            conditions=[
                lambda effect, message:
                    Condition.PlayerInMassForm(CardFinder(name="Vision"), "Dense", effect),
            ]
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            solar_beam_thwart,
            conditions=[
                lambda effect, message:
                    Condition.PlayerInMassForm(CardFinder(name="Vision"), "Intangible", effect),
            ]
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

