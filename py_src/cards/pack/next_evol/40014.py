from . import *

# * Caliban

def GetAbilities() -> Sequence['Ability']:

    def caliban(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = initiator.DiscardDeckUntil(
            effect,
            card_type=Ally,
            check_face_fn=lambda face:
                face.HasTrait("X-FACTOR", "X-FORCE", "X-MEN"),
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            caliban,
        ),
    ]

