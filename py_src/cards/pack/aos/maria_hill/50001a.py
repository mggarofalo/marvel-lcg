from . import *

# * Maria Hill

def GetAbilities() -> Sequence['Ability']:

    def maria_hill(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.RemoveCountersOn(effect.targets, 1, 'all-purpose', effect)
        faces = Worlds.FindCardsOnField(effect, trait="S.H.I.E.L.D", card_type=Support)
        faces = [x for x in faces if x not in effect.targets]
        face = initiator.AskChooseFace(faces, effect)
        if face:
            Faces.PlaceCountersOn([face], 1, 'all-purpose', effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Ally,
            control_by="You",
            trait="S.H.I.E.L.D",
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            maria_hill
        ).SetName("Reassignment")
        .SetTarget(Support, trait="S.H.I.E.L.D", has_counter='all-purpose')
        .SetTarget2(Support, trait="S.H.I.E.L.D", range=(2, 2), is_optional=False)
        .LimitOncePerRound(),
    ]

