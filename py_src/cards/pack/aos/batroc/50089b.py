from . import *

# Extract Captives

def GetAbilities() -> Sequence['Ability']:

    def extract_captives(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        allies = Worlds.FindCardsOnField(effect, name="Rescued Captive")
        ally = Filter.One(allies, effect)
        if ally:
            message.ReplaceTarget(ally)

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            Enemy,
            extract_captives
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            quickstrike=1,
            expert_mode_only=True,
        ),
        AbilityFactory.IfNoThreatHerePlayersWinTheGame(),
        AbilityFactory.IfThisSchemeStageIsCompletedPlayersLoseTheGame(),
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.NonKeywordBold,
            CardFinder(name="Rescued Captive"),
            lambda effect, message:
                Worlds.SetGameOver(False, effect),
            conditions=[
                lambda effect, message:
                    Worlds.FindCardSizeOnField(effect, name="Rescued Captive") == 0
            ]
        ),
    ]

