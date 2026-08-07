from . import *

# Uncanny X-Men

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("X-MEN", Ally),
            control_by="You",
            health=1,
            change_on_event=OnEvent.Trait("YouControlCharacter")
        ),
        AbilityFactory.ReduceCostToPlayFaceWhen(
            CardFinder2("X-MEN", Ally),
            1,
            "You",
            conditions=[
                lambda effect, message:
                    Faces.IsAll(effect.GetInitiator().GetControlCharacters(), CardFinder2("X-MEN")),
            ]
        )
    ]

