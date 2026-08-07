from . import *

# * Shadowcat: Kitty Pryde

def GetAbilities() -> Sequence['Ability']:

    def shadowcat(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for scheme in effect.targets:
            if SchemeSide2.IsType(scheme):
                scheme.IgnoreKeywordUntilRoundEnd(effect, "Acceleration", "Amplify", "Crisis", "Hazard")

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            shadowcat,
        ).SetTarget(SchemeSide2),
    ]

