from . import *

# Head of Steam

def GetAbilities() -> Sequence['Ability']:

    def head_of_steam_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )
        if villain:
            this.AttachTo2(villain, effect)
            Faces.PlaceCountersOn([villain], 1, 'momentum', effect)

    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Juggernaut"),
            get_new_value=lambda effect, attach, ui:
                attach.CastTo(Villain).GetCounters('momentum'),
            retaliate=1,
            ex_change_on_event=OnEvent.Counter(CardFinder(name="Juggernaut"), 'momentum'),
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            head_of_steam_revealed
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.HeroResponse,
            CardFinder(name="Juggernaut"),
            DiscardThisCard
        ).SetCost(lambda effect, faces: Cost("1") * effect.GetBindMessage(Message.AfterUnitAttackEnd).total_dealt_damage),
    ]

