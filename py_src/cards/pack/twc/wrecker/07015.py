from . import *

# Mystical Link

def GetAbilities() -> Sequence['Ability']:

    def mystical_link(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        for scheme in Worlds.GetSideSchemes(effect):
            this.PlaceThreatOnSchemes([scheme], 2, effect)

    def mystical_link_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        player = message.GetToPlayer()

        villain = message.being_message.by_face
        if Villain.IsType(villain):
            scheme = villain.FindCorrespondingScheme()
            if scheme:
                if not player.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Place 2 threat on his side scheme",
                        lambda targets:
                            this.PlaceThreatOnSchemes(targets, 2, effect)
                    ).SetTarget([scheme])
                ):
                    villain.GainForThisActive(effect, message.being_message, attack=+3)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mystical_link
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            mystical_link_boost
        )
    ]

