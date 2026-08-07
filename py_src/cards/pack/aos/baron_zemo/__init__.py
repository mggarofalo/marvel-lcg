from cards.pack import *

class Buff_50168a(Buff):

    def __init__(self) -> None:
        super().__init__()
        self.means = ""
        self.motive = ""
        self.opportunity = ""
        self.accused: 'CardFace'

def RemoveSecretCountersFromAmongBoardMemberEnvironments(size: int|Literal["1*"], by_effect: 'Effect'):
    player = Worlds.GetFirstPlayer(by_effect)

    faces = Worlds.FindCardsOnField(by_effect, has_counter='secret', card_type=CanPlaceCounter)
    faces = [x for face in faces for x in [face] * face.GetCounters('secret')]

    size = Worlds.ConvertPerPlayerIconToInt(size, by_effect)

    player.ChooseAbilities(
        by_effect,
        AbilityFactory.ForChoiceAbility(
            "",
            lambda targets:
                Faces.RemoveCountersOn(targets, 1, 'secret', by_effect)
        ).SetTarget(faces, range=(0, size))
    )

def PlaceSecretCounterOnEachBoardMemberCard(size: int, by_effect: 'Effect'):
    faces = Worlds.FindCardsOnField(by_effect, trait="BOARD MEMBER")
    Faces.PlaceCountersOn(faces, size, 'secret', by_effect)

def PlaceSecretCounterOnBoardMemberCard(card_type: Type[Environment|CardFace], size: int, by_effect: 'Effect', with_fewest_secret: bool=False):
    faces = Worlds.FindCardsOnField(by_effect, trait="BOARD MEMBER", card_type=card_type)

    if with_fewest_secret:
        fewest_counter = 'secret'
    else:
        fewest_counter = False

    face = Filter.One(faces, by_effect, fewest_counter=fewest_counter)

    if face:
        Faces.PlaceCountersOn([face], size, 'secret', by_effect)


def BaronZemoA(health: int):

    def baron_zemo(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.SetBeInstead(effect)

        this.ResetHealth(effect, health)
        RemoveSecretCountersFromAmongBoardMemberEnvironments(3, effect)


    return AbilityFactory.WhenUnitWouldBeDefeated(
        AbilityType.ForcedInterrupt,
        "This",
        baron_zemo
    )

def BaronZemoB(secret: int):
    
    def baron_zemo(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                f"Place {secret} secret counter on a [[Board Member]] card",
                lambda targets:
                    Faces.PlaceCountersOn(targets, secret, 'secret', effect)
            ).SetTarget(trait="BOARD MEMBER"),
            AbilityFactory.ForChoiceAbility(
                "Give Baron Zemo an additional boost card for this activation",
                lambda targets:
                    message.GiveAdditionalBoostCardForThisActivation(1, effect)
            ),
        )


    return AbilityFactory.WhenEnemyActivateAgainstYou(
        AbilityType.ForcedInterrupt,
        "This",
        baron_zemo
    )

def ShieldEnvelopeHasCards(by_effect: 'Effect'):
    deck = Worlds.ScenarioDeck(by_effect, "S.H.I.E.L.D.Envelope")
    return deck.GetTopCard() != None

def GainCardsFromShieldEnvelope(size: int, by_effect: 'Effect'):
    deck = Worlds.ScenarioDeck(by_effect, "S.H.I.E.L.D.Envelope")
    faces = deck.GetTopCards(size)
    Faces.FlipAllTo(faces, True, by_effect)
    for face in faces:
        face.PutIntoPlay("FirstPlayer", by_effect)

"""
PREPARING THE EVIDENCE
To determine the mole at the start of the campaign, follow these steps:
1. Separate the nine evidence cards ( 185–193) by their card backs into three sets of three cards.
2. Shuffle each set of three cards separately and put one card from each set into the A.I.M. envelope without looking at them.
    » Insert the cards facedown with the side of the envelope with the “A.I.M.” logo faceup so that you can later remove the cards from the envelope without seeing their faces.
3. Shuffle the six remaining evidence cards together and put them in the S.H.I.E.L.D. envelope without looking at them.
    » Insert the cards facedown with the side of the envelope with the “S.H.I.E.L.D.” logo faceup so that you can later remove the cards from the envelope without seeing their faces.
"""
def PreparingTheEvidence(effect: 'Effect'):
    faces = Worlds.GetEncounterDeckCards(effect) + Worlds.GetEncounterDiscardPileCards(effect)
    evidences = Filter.ByType(faces, Evidence)

    def select_one_of_each_type(objects: Sequence['Evidence']):
        # Initialize a list to keep track of selected objects and a set for types
        selected_objects: List['Evidence'] = []
        selected_types: Set[Evidence.SUBTYPE] = set()

        # Iterate through the list and select one of each type
        for obj in objects:
            obj_type = obj.subtype
            if obj_type not in selected_types:
                selected_objects.append(obj)
                selected_types.add(obj_type)

            # Stop if we have selected one of each type
            if len(selected_types) == 3:
                break

        return selected_objects

    faces = select_one_of_each_type(evidences)

    villain = Worlds.FindVillain(effect)
    assert villain
    deck1 = Worlds.ScenarioDeck(effect, "A.I.M.Envelope")
    deck1.Create(effect, villain)
    deck1.PushCards(faces, effect)

    deck2 = Worlds.ScenarioDeck(effect, "S.H.I.E.L.D.Envelope")
    deck2.Create(effect, villain)
    deck2.PushCards([x for x in evidences if x not in faces], effect)

def CalcMole(by_effect: 'Effect') -> 'CardFace':
    ORANGE1 = "50185"
    BLUE1   = "50186"
    RED1    = "50187"
    GREEN2  = "50188"
    BLACK2  = "50189"
    YELLOW2 = "50190"
    PURPLE3 = "50191"
    RED3    = "50192"
    BLUE3   = "50193"

    ChiefMedicalOfficer = [
        [ORANGE1,   GREEN2,     PURPLE3],
        [ORANGE1,   GREEN2,     RED3],
        [ORANGE1,   BLACK2,     PURPLE3],
        [ORANGE1,   BLACK2,     BLUE3],
        [ORANGE1,   YELLOW2,    RED3],
        [BLUE1,     GREEN2,     PURPLE3],
        [BLUE1,     GREEN2,     RED3],
        [BLUE1,     BLACK2,     PURPLE3],
        [RED1,      GREEN2,     PURPLE3],
    ]

    ChiefSurveillanceOfficer = [
        [ORANGE1,   BLACK2,     RED3],
        [BLUE1,     GREEN2,     BLUE3],
        [BLUE1,     BLACK2,     RED3],
        [BLUE1,     BLACK2,     BLUE3],
        [BLUE1,     YELLOW2,    PURPLE3],
        [BLUE1,     YELLOW2,    RED3],
        [RED1,      BLACK2,     RED3],
        [RED1,      BLACK2,     BLUE3],
        [RED1,      YELLOW2,    RED3],
    ]

    ChiefTacticalOfficer = [
        [ORANGE1,   GREEN2,     BLUE3],
        [ORANGE1,   YELLOW2,    PURPLE3],
        [ORANGE1,   YELLOW2,    BLUE3],
        [BLUE1,     YELLOW2,    BLUE3],
        [RED1,      GREEN2,     RED3],
        [RED1,      GREEN2,     BLUE3],
        [RED1,      BLACK2,     PURPLE3],
        [RED1,      YELLOW2,    PURPLE3],
        [RED1,      YELLOW2,    BLUE3],
    ]

    deck = Worlds.ScenarioDeck(by_effect, "A.I.M.Envelope")
    faces = deck.deck.GetAll()
    card_ids = [x.paper.card_id for x in faces]
    card_ids.sort()

    if card_ids in ChiefMedicalOfficer:
        face = Worlds.FindCardOnField(by_effect, name="Chief Medical Officer")
        if face:
            return face
        face = Worlds.FindCardOnField(by_effect, name="Medical Officer's Aid")
        assert face
        return face

    if card_ids in ChiefSurveillanceOfficer:
        face = Worlds.FindCardOnField(by_effect, name="Chief Surveillance Officer")
        if face:
            return face
        face = Worlds.FindCardOnField(by_effect, name="Surveillance Officer's Aid")
        assert face
        return face

    if card_ids in ChiefTacticalOfficer:
        face = Worlds.FindCardOnField(by_effect, name="Chief Tactical Officer")
        if face:
            return face
        face = Worlds.FindCardOnField(by_effect, name="Tactical Officer's Aid")
        assert face
        return face

    assert False

