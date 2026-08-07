from . import *

# Take the Fight to Them

def GetAbilities() -> Sequence['Ability']:

    def take_the_fight_to_them(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck("EncounterDeck", "2*", effect)

        discards = initiator.AskDiscardFaces(faces, "Any", effect)
        rest = Faces.GetRest(faces, discards)
        initiator.PlaceOnTopAndOrBottomInAnyOrder(rest, effect)

        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            take_the_fight_to_them
        ).SetPlay().SetLabel()
        .SetHasNoTargetEffect(),
    ]

