from . import *

# * Captain Marvel: Carol Danvers

def GetAbilities() -> Sequence['Ability']:

    def captain_marvel(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.DiscardDeckTopCards(4, effect)
        energy = FacesCounter.GetPrintedResourcesCount(faces, "Y")

        def action(targets: Sequence['CardFace']):
            this.DealDamage(targets, 3, effect)
            if energy > 1:
                Faces.GiveStatus(targets, "Stunned", effect)

        if energy:
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Deal 3 damage to an enemy",
                    action
                ).SetTarget(Enemy)
            )


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            captain_marvel
        ),
    ]

