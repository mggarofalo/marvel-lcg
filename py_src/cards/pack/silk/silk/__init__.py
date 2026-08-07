from cards.pack import *

def IfThereAreMoreThan4TuckedCardsHere():

    def silk(effect: 'Effect', message: 'Message.AfterCardsMoved') -> None:
        this = effect.this

        size = this.GetPlacedCardArea().GetSize() - 4
        faces = this.GetPlacedCardArea().GetAll()
        player = this.GetControlByPlayer()
        player.AskDiscardFaces(faces, (size, size), effect)

    return AbilityFactory.AfterCardsMoved(
        AbilityType.NonKeyword,
        None,
        silk,
        conditions=[
            lambda effect, message:
                any(x.flags.is_place_card_area for x in message.into_areas) and \
                effect.this.GetPlacedCardArea().GetSize() > 4
        ]
    )

def TuckCardUnderSilk(face: 'CardFace', by_effect: 'Effect'):
    silk = Worlds.FindCardOnField(by_effect, name="Silk")
    if silk:
        silk.TuckCardUnderHere(face, by_effect)

def TuckCardUnderCindyMoon(faces: Sequence['CardFace'], by_effect: 'Effect'):
    silk = Worlds.FindCardOnField(by_effect, name="Cindy Moon")
    if silk:
        for face in faces:
            face.card.visible.Set(silk.GetControlByPlayer())
        silk.TuckCardUnderHere(faces, by_effect)

def YouMayDiscardACardTuckedUnderSilkFromTheSameSet(initiator: 'Player', same_as_faces: Sequence['CardFace'], by_effect: 'Effect') -> bool:
    faces = initiator.GetIdentity().GetPlacedCardArea().GetAll()
    set_names = set([x.paper.set_name for x in same_as_faces])
    faces = [x for x in faces if x.paper.set_name in set_names]

    discard_effect = initiator.MayChooseOneAbility(
        by_effect,
        AbilityFactory.ForChoiceAbility(
            "Discard a card tucked under Silk from the same set",
            lambda targets:
                Faces.DiscardAll(targets, by_effect)
        ).SetTarget(faces)
    )
    return discard_effect != None

def GetTuckCardSameSetSize(initiator: 'Player', targets: Sequence['CardFace'], by_effect: 'Effect') -> int:
    faces = initiator.GetIdentity().GetPlacedCardArea().GetAll()
    set_names = set([x.paper.set_name for x in targets])
    faces = [x for x in faces if x.paper.set_name in set_names]

    return len(faces)

