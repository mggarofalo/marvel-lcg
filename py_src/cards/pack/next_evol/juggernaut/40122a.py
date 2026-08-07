from . import *

# * Juggernaut's Helmet

def GetAbilities() -> Sequence['Ability']:

    def juggernauts_helmet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )
        if villain:
            Faces.RemoveCountersOn([villain], "All", 'momentum', effect)

        this.card.Flip(effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Juggernaut")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Juggernaut"),
            stalwart=1,
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Juggernaut"),
            overkill=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            juggernauts_helmet,
        ).SetCost(Cost("3", same_type=True))
    ]

