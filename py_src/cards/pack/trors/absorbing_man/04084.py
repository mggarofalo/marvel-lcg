from . import *

# * Ball and Chain

def GetAbilities() -> Sequence['Ability']:

    def ball_and_chain(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        encounter_deck = Worlds.GetEncounterDeck(effect)
        Faces.ShuffleAllTo([this], encounter_deck, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Absorbing Man")
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ball_and_chain
        ).SetCost(Cost("R")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        )
    ]

