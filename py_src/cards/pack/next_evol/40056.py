from . import *

# * Atlas Bear: Shoon'kwa

def GetAbilities() -> Sequence['Ability']:

    def atlas_bear(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck(initiator.player_deck, 1, effect)
        for face in faces:
            if Event.IsType(face):

                def action(targets: Sequence['CardFace']):
                    this.DealDamage([this], 1, effect)
                    for face in targets:
                        owner = face.GetOwner()
                        if isinstance(owner, Player):
                            owner.GainCard(face, effect)

                initiator.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Deal 1 damage to Atlas Bear to add that card to its owner's hand",
                        action
                    ).SetTarget([face], range="All", not_shuffle=True, not_move=True)
                )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            atlas_bear
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

