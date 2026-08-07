from . import *

# Manipulated Mind

def GetAbilities() -> Sequence['Ability']:

    def manipulated_mind_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlAllies()
        face = Filter.One(faces, effect, lowest_cost=True)
        if face:
            this.AttachTo2(face, effect)
        else:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "Minion",
            lambda minion, ally, effect:
                minion.CopyTrait(ally, effect)
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            manipulated_mind_revealed
        ),
    ]

