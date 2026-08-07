from cards.pack import *

def AbilityDealDamageToAvengersTower(damage: int, by_effect: 'Effect') -> 'Ability':
    this = by_effect.this
    Unused(this)

    def invoke_damage(targets: Sequence['CardFace']):
        Faces.PlaceCountersOn(targets, damage, 'damage', by_effect)

    return AbilityFactory.ForChoiceAbility(
        f"deal {damage} damage to Avengers Tower",
        invoke_damage
    ).SetTarget(name="Avengers Tower")

def DealDamageToAvengersTower(damage: int|Literal["6*"], by_effect: 'Effect'):
    this = by_effect.this
    Unused(this)

    face = Worlds.FindCardOnField(by_effect, CardFinder(name="Avengers Tower"))
    if face:
        Faces.PlaceCountersOn([face], damage, 'damage', by_effect)

def CheckVillainHealth(name: str, effect: 'Effect') -> int:
    face = Worlds.FindCardOnField(
        effect,
        name=name,
        card_type=Villain
    )
    if face:
        return face.health
    else:
        return 0

