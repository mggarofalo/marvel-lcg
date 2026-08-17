from . import *

# All Hail King Loki

def GetAbilities() -> Sequence['Ability']:

    def all_hail_king_loki(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        # At this timming, the villain hasn't been moved to victory display
        num = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(name="Loki"))
        target_num = 1
        if Worlds.IsExpert(effect):
            target_num = 2
        if num == target_num:
            Worlds.SetGameOver(True, effect)
            return

        Worlds.GetScenario(effect).villain_deck.Shuffle(effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            Villain,
            all_hail_king_loki
        ),
        # The engine's imported card text omits this printed sentence:
        # "If this stage is completed, the players lose the game."
        AbilityFactory.IfThisSchemeStageIsCompletedPlayersLoseTheGame(),
    ]
