from . import *

# * Falcon: Sam Wilson

def GetAbilities() -> Sequence['Ability']:

    def falcon(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck("EncounterDeck", 3, effect)
        size = Faces.FindCardSize(faces, CardFinder(card_type=Treachery))

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, 1, effect)
            ).SetTarget(Scheme2, range=("Zero", size), repeat_rules="Threat")
        )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            falcon
        )
    ]

