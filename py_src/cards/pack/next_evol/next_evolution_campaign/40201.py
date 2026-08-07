from . import *

# * Vanisher

def GetAbilities() -> Sequence['Ability']:

    def vanisher(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.supports.Get()
        face = Filter.One(faces, effect, highest_cost=True)
        if face:
            Faces.ReturnToHand([face], player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            vanisher
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            vanisher
        ),
    ]

