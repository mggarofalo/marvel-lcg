from cards.pack import *

def ChooseSetAsideVillainAtRandom(effect: 'Effect') -> 'Villain|None':
    villains = [x for x in Worlds.GetSetAsideAreaCards(effect) if Villain.IsType(x)]
    if villains:
        return Rand.RandomChoice(villains, effect)
    return None

def ResolveMainSchemeAmbush(player: 'Player', effect: 'Effect'):
    this = effect.this
    scheme = Worlds.FindMainScheme(this)
    if scheme:
        scheme.ResolveSpecialAbility(player, effect, name="Ambush!")

def ResolveMainSchemeAmbushAndAttach(effect: 'Effect', player: 'Player'):
    ResolveMainSchemeAmbush(player, effect)
    this = effect.this.CastTo(Asset2)
    villain = Worlds.FindVillain(effect)
    if villain:
        this.AttachTo2(villain, effect)

def GetLightAtTheEnd(effect: 'Effect'):
    face = Worlds.FindCardOnField(
        effect,
        name="Light at the End",
        card_type=SchemeSide2
    )
    assert face != None
    return face

def MakeActivationOrderTrait(i: int) -> 'CardFace.TRAITS':
    trait = f"ACTIVATION ORDER {i}"
    assert IsTrait(trait)
    return trait

def PlaceActiveCounterOnLowestActivationOrder(villains: List['Villain'], effect: 'Effect'):
    for i in range(1, 7):
        for villain in villains:
            trait = MakeActivationOrderTrait(i)
            if villain.HasTrait(trait):
                villain.SetActive(effect)
                break
        else:
            continue
        break

def MoveTheActiveCounterToTheNextVillain(effect: 'Effect'):
    order = 0
    villain = None
    for villain in Worlds.GetVillains(effect):
        if villain.IsActive():
            break

    if not villain:
        return

    for i in range(1, 7):
        trait = MakeActivationOrderTrait(i)
        if villain.HasTrait(trait):
            order = i
            break

    # this.RemoveCounters(1, 'active', True, effect)

    assert order != 0
    for i in range(order+1, order+7):
        if i > 6:
            i -= 6
        for villain in Worlds.GetVillains(effect):
            trait = MakeActivationOrderTrait(i)
            if villain.HasTrait(trait):
                villain.SetActive(effect)
                break
        else:
            continue
        break


def SinisterSixWhenDefeated():
    def when_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        value = 4
        if len(Worlds.GetVillains(effect)) == 1:
            value = 7
        first_player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.RemoveThreatFromSchemes(targets, value, effect),
            ).SetTarget(SchemeSide2)
        )
        Faces.SetAside([this], effect)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.WhenDefeated,
        "This",
        when_defeated
    )

def GetHighestOrder(villains: Sequence[CardFace]) -> Sequence[Villain]:
    for i in range(6, 0, -1):
        for villain in villains:
            trait = MakeActivationOrderTrait(i)
            if villain.HasTrait(trait) and \
                Villain.IsType(villain):
                return [villain]
    return []

def GetLowestOrder(villains: Sequence[CardFace]) -> Sequence[Villain]:
    for i in range(1, 7):
        for villain in villains:
            trait = MakeActivationOrderTrait(i)
            if villain.HasTrait(trait) and \
                Villain.IsType(villain):
                return [villain]
    return []

def ThePlayersCannotWinUnlessTheyEscap() -> 'Ability':
    return AbilityFactory.WhenCardEnterPlay(
        AbilityType.NonKeyword,
        "This",
        lambda effect, message: None
    )

def PutSetasideVillainIntoPlay(name: str, by_effect: 'Effect'):
    player = Worlds.GetFirstPlayer(by_effect)

    face = Search.SearchForCard(
        by_effect,
        player,
        include_set_aside=True,
        name=name,
    )
    if face:
        face.PutIntoPlay(player, by_effect)

