from . import *

# Juggernaut Exposed

def GetAbilities() -> Sequence['Ability']:

    def juggernaut_exposed(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )
        if villain:
            Faces.PlaceCountersOn([villain], 1, 'momentum', effect)

        this.card.Flip(effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Juggernaut")
        ),
        AbilityFactory.UnitTakeAdditionalDamageFromCards(
            CardFinder(name="Juggernaut"),
            +1,
            CardFinder(has_printed_res="B"),
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="Juggernaut"),
            juggernaut_exposed
        ),
    ]

