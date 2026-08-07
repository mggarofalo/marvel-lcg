from . import *

# Breakout - 1B

def GetAbilities() -> Sequence['Ability']:

    def breakout(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        for scheme in Worlds.GetSideSchemes(effect):
            this.PlaceThreatOnSchemes([scheme], 1, effect)

        # Move the active counter to the villain whose scheme has the most threat.
        # (If there is a tie, the first player chooses.)
        most_threat_schemes: List[SchemeSide2] = []
        for name in SCHEMES_VILLAINS:
            scheme = Worlds.FindCardOnField(
                effect,
                name=name,
                card_type=SchemeSide2
            )
            if scheme:
                if most_threat_schemes == []:
                    most_threat_schemes = [scheme]
                elif scheme.threat == most_threat_schemes[0].threat:
                    most_threat_schemes.append(scheme)
                elif scheme.threat > most_threat_schemes[0].threat:
                    most_threat_schemes = [scheme]

        def set_most_threat(targets: Sequence['CardFace']):
            villain: Villain|None = None
            scheme = targets[0].CastTo(EncounterSideScheme)
            villain = scheme.FindCorrespondingVillain()
            if villain:
                villain.SetActive(effect)

        first_player = Worlds.GetFirstPlayer(effect)
        first_player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                set_most_threat
            ).SetTarget(most_threat_schemes)
        )

    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            breakout
        )
    ]

