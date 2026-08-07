from . import *

# Even the Odds

def GetAbilities() -> Sequence['Ability']:

    def even_the_odds(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        defeated = 0

        def action():
            nonlocal defeated
            defeated += 1

        this.RemoveThreatFromSchemes(effect.targets, "1*", effect, if_this_remove_last_threat=action)

        if defeated > 0:
            this.DealDamage(effect.targets2, defeated, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            even_the_odds
        ).SetPlay().SetLabel('thwart')
        .SetTarget(SchemeSide2, range="All")
        .SetTarget2(Villain),
    ]

