from . import *

# * Madame Web: Julia Carpenter

def GetAbilities() -> Sequence['Ability']:

    def madame_web(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        value = len(initiator.GetControlCards2(CardFinder2("WEB-WARRIOR")))
        faces = initiator.LookAtDeck("EncounterDeck", value, effect)

        def action(targets: Sequence[CardFace]):
            Faces.DiscardAll(targets, effect)

            rest = [x for x in faces if x not in targets]
            initiator.PlaceOnTopInAnyOrder(rest, effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard 1 card looked at this way and put the rest back in any order",
                action,
            ).SetTarget(faces, canbe_discard=True, not_move=True)
        )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            madame_web
        ),
    ]

