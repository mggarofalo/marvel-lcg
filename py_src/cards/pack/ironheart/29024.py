from . import *

# * Vivian

def GetAbilities() -> Sequence['Ability']:

    def vivian(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        targets = effect.targets[:]

        for target in targets:
            target.TreatAsIfBlank(effect, until_round_end=True)

    def attachment_non_elite_minion_or_non_permanent_side_scheme(effect: 'Effect'):
        faces: List[CardFace] = []
        for face in Worlds.GetOnFieldCards(effect):
            if Attachment.IsType(face):
                faces.append(face)
            elif Minion.IsType(face) and not face.HasTrait('ELITE'):
                faces.append(face)
            elif SchemeSide2.IsType(face) and HasPermanent.IsType(face) and not face.permanent:
                faces.append(face)
        return faces


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            vivian
        ).SetTarget(attachment_non_elite_minion_or_non_permanent_side_scheme),
    ]

