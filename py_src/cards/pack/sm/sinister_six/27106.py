from . import *

# Take One for the Team

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain,
            highest_atk=True,
            if_cannot_operation=ResolveMainSchemeAmbushAndAttach
        ),
        AbilityFactory.PlayersCannotAttackWhile(
            "AnyPlayer",
            Villain,
            conditions=[
                lambda effect, message:
                    not message.being_attack.GetInventoryDeck().FindCardSize(CardFinder(name=effect.this.name, card_type=type(effect.this))),
            ]
        )
    ]

