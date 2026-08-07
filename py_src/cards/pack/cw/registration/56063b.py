from . import *

# Cut Off Support

def GetAbilities() -> Sequence['Ability']:

    def cut_off_support(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], 1, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            check_fn=lambda effect, ui:
                Worlds.GetYourTeamSize(effect) > 1,
            hinder="2*",
        ),
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.ForcedInterrupt,
            "AnyPlayer",
            Treachery,
            None,
            cut_off_support
        ).LimitOncePerPhasePerPlayer(),
    ]

