from . import *

# Combined Effort

def GetAbilities() -> Sequence['Ability']:

    def combined_effort_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            ThisCardGainSurge(effect)

        Worlds.Enemies.AllMinionActivateAgainstEngagedPlayer(effect, trait="ELITE", if_no_activate=action)

    def combined_effort_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = Worlds.FindCardSizeOnField(effect, CardFinder2("ELITE", Minion))
        message.GainBoostIcon(value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            combined_effort_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            combined_effort_boost
        ),
    ]

