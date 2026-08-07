from . import *

# Zemo's Manipulations

def GetAbilities() -> Sequence['Ability']:

    def zemos_manipulations(effect: 'Effect', message: 'Message.AfterPhaseEnd') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        if ShieldEnvelopeHasCards(effect):
            if Worlds.IsCampaign(effect):
                size = 1
            else:
                size = 2

            choose_effect = player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Place 2 secret counters on a [[Board Member]] environment",
                    lambda targets:
                        Faces.PlaceCountersOn(targets, 2, 'secret', effect)
                ).SetTarget(Environment, trait="BOARD MEMBER", check_fn=lambda effect, face: face.CastTo(Environment).GetAllCounters() == 0)
            )

            if choose_effect:
                GainCardsFromShieldEnvelope(size, effect)

        player.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Advance to stage 2A",
                lambda targets:
                    this.Advance("2A", effect)
            )
        )

    return [
        AbilityFactory.AfterPhaseEnd(
            AbilityType.Response,
            "Player",
            zemos_manipulations
        ),
    ]

