from . import *

# * Black Cat

def GetAbilities() -> Sequence['Ability']:

    def black_cat(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.DiscardDeckTopCards(2, effect)
        faces = CardFinder(has_printed_res="B").Checks(faces)
        initiator.GainCard(faces, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.ForcedResponse,
            "You",
            "This",
            black_cat
        )
    ]

