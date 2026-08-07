from cards.pack import *

def TheMadDoctor() -> 'Ability':

    def the_mad_doctor(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'test', effect)
        if this.GetCounters('test') >= 3:
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion
            )
            if minion:
                first_player = Worlds.GetFirstPlayer(effect)
                minion.PutIntoPlay(first_player, effect)
                Faces.RemoveCountersOn([this], 3, 'test', effect)

    return AbilityFactory.AfterResolveVillainPhaseStep(
        AbilityType.ForcedResponse,
        1,
        the_mad_doctor
    )

# def SelectMinionWithMostRemainingHpWithoutThisAttached(effect: Effect) -> List['Minion']:
#     this = effect.this.CastTo(Attachment)
#     unit = FindMostHpUnit([x for x in Worlds.GetOnFieldMinions(effect) if x.GetUpgradeDeck().FindCard(name=this.name) == None])
#     return [unit] if unit else []

