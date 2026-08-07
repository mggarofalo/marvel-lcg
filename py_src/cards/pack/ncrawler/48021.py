from . import *

# * Gambit: Remy LeBeau

def GetAbilities() -> Sequence['Ability']:

    def gambit(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck("EncounterDeck", 3, effect)
        face = initiator.AskChooseFace(faces, effect)
        if face:
            this.TuckCardUnderHere(face, effect)

    def gain_boost(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Ally)
        faces = this.GetPlacedCardArea().Get()
        ui.extend(faces)
        value = FacesCounter.CountTotalBoostIcons(faces)
        return value

    return [
        AbilityFactory.ThisGainKeyword(
            gain_boost,
            attack=1,
            thwart=1,
            change_on_event=OnEvent.TuckUnder("Here")
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            gambit
        ),
    ]

