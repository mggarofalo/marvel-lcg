from . import *

# Slipping Sanity

def GetAbilities() -> Sequence['Ability']:

    def slipping_sanity(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            faces = Worlds.DiscardEncounterCards(5, effect)
            value = 0
            for face in faces:
                if HasBoostIcon.IsType(face):
                    value += face.boost_star
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                this.PlaceThreatOnSchemes([scheme], value, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Wanda Maximoff → remove Slipping Sanity from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard the top 5 cards of the encounter deck",
                action,
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            slipping_sanity
        )
    ]

