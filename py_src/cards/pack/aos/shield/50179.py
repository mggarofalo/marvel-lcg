from . import *

# Arrest Warrant

def GetAbilities() -> Sequence['Ability']:

    def arrest_warrant(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        player = effect.GetInitiator()

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            trait="S.H.I.E.L.D",
            card_type=Minion
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
            ex_operation=arrest_warrant
        ).SetCostFunc(CostFunc.Exhaust(Select.From("YouControlCards", finder=CardFinder(card_type=Identity)|CardFinder2("S.H.I.E.L.D")))),
    ]

