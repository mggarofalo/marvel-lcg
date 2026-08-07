from core import *
from game.card.face.card_face import CardFace
from game.card.face.base import ClassCard

CARD_CLASS_LIST: List[ClassCard.CARD_CLASS] = Types.LiteralToList(ClassCard.CARD_CLASS)
ASPECT_CLASS_LIST: List[ClassCard.ASPECT_CLASS_TYPE] = Types.LiteralToList(ClassCard.ASPECT_CLASS_TYPE)

def IsTrait(a: str) -> TypeGuard['CardFace.TRAITS']:
    return a in CardFace.TRAITS_LIST

def IsCounter(a: str) -> TypeGuard['CardFace.COUNTER']:
    return a in CardFace.COUNTER_LIST

def IsCardClass(a: str) -> TypeGuard['ClassCard.CARD_CLASS']:
    return a in CARD_CLASS_LIST

def IsAspectClass(a: str) -> TypeGuard['ClassCard.ASPECT_CLASS_TYPE']:
    return a in ASPECT_CLASS_LIST

