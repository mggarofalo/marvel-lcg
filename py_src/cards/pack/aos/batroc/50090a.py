from . import *

# Alert Level

def GetAbilities() -> Sequence['Ability']:

    def alert_level(effect: 'Effect', message: 'Message.AfterCardPlacedToken') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.RemoveTokensOn([this], "All", 'threat', effect)
        this.card.Flip(effect)

    def alert_level_response(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.PlaceTokensOn([this], 1, 'threat', effect)

    def alert_level_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.RemoveTokensOn([this], 1, 'threat', effect)


    return [
        AbilityFactory.IfThereIsAtLeastThreatHere(
            "4*",
            alert_level,
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            "Character",
            alert_level_response,
            by_consequential=False,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            alert_level_action,
        ).SetCost(Cost("1")),
    ]

