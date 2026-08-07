from . import *

# * Sentry: Robert Reynolds

def GetAbilities() -> Sequence['Ability']:

    def sentry(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        scheme = Search.EncounterCard(
            effect,
            initiator,
            include_discard_pile=False,
            card_type=SchemeSide2
        )
        if scheme:
            scheme.Reveal(initiator, effect)
        else:
            this.PlaceThreatOnSchemes("MainScheme", 6, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            sentry
        ),
    ]

