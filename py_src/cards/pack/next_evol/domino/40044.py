from . import *

# * Pip the Pug

def GetAbilities() -> Sequence['Ability']:

    def pip_the_pug(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.MoveAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            pip_the_pug
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(check_fn=lambda effect, face:
            ClassCard.IsType(face) and face.IsDeckBuildingClass("Domino") or \
            face.HasTrait("POSSE"),
            from_where=["YourDiscardPile"]),
    ]

