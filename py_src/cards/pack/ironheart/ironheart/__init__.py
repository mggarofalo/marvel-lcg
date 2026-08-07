from cards.pack import *

def GetIronheartVersionNumber(player: 'Player') -> int:
    identity = player.GetIdentity()
    if identity.HasTrait("VERSION 3"):
        return 3
    if identity.HasTrait("VERSION 2"):
        return 2
    if identity.HasTrait("VERSION 1"):
        return 1
    return 0

def LevelUp(to_level: Literal[2, 3], give_tough: bool) -> 'Ability':

    def ironheart(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

        if give_tough:
            Faces.GiveStatus(effect.targets, "Tough", effect)

        if to_level == 2:
            id = "29002a"
        elif to_level == 3:
            id = "29003a"
        else:
            assert False

        for face in this.card.back_faces:
            if face.IsName(id):
                assert Hero.IsType(face)
                # Change to face will trigger the effect on "04093"
                # this.ChangeToFace(face, effect)
                this.FlipTo(effect, card_face=face, by_swapping=True)
                break


    return AbilityFactory.WhenInYourPlayTurn(
        AbilityType.Action,
        ironheart
    ).SetName("Level Up!") \
    .SetCostFunc(CostFunc.Counter("This", 6, 'progress')) \
    .SetTarget("This")

def ChildProdigy(level: int) -> 'Ability':
    
    def riri_williams(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'progress', effect)

    if level == 1:
        res = Cost("B")
    elif level == 2:
        res = Cost("B", or_cost=Cost("2"))
    elif level == 3:
        res = Cost("1")
    else:
        assert False, f"{level=}"

    return AbilityFactory.WhenInYourPlayTurn(
        AbilityType.Action,
        riri_williams
    ).SetName("Child Prodigy") \
    .SetCost(res) \
    .SetTarget(name="Riri Williams") \
    .LimitOncePerRound()

