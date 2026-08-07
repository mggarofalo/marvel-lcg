from cards.pack import *

def Loki() -> 'Ability':
    def loki(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(None, effect)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.WhenDefeated,
        "This",
        loki
    )

def GetSetAsideLokiVillains(effect: 'Effect') -> List['Villain']:
    faces = [x for x in Worlds.GetSetAsideAreaCards(effect) if Villain.IsType(x)] + Worlds.GetScenario(effect).villain_deck.Get()
    villains = CardFinder(name="Loki", card_type=Villain).Checks(faces)
    return villains

def GetRandomSetAsideLokiVillain(effect: 'Effect') -> 'Villain':
    return Rand.RandomChoice(GetSetAsideLokiVillains(effect), effect)

def SwapLokiWithRandomSetAsideLokiVillain(by_effect: 'Effect'):
    loki = Worlds.FindVillain(by_effect, name="Loki")
    if loki:
        next_villain = GetRandomSetAsideLokiVillain(by_effect)
        loki.card.SwapCard(next_villain, by_effect)
        # next_villain.AfterAdvanced()
        scenario_message = Message.WhenScenarioEvent("SwapLokiWithRandomSetAsideLokiVillain", world=by_effect.world)
        scenario_message.Send()
    pass
    
from cards.pack.mts.infinity_gauntlet import GetInfinityStone
Unused(GetInfinityStone)

