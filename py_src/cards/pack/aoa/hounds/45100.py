from . import *

# Release the Hounds

def GetAbilities() -> Sequence['Ability']:

    def release_the_hounds(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        minion = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Hound",
            card_type=Minion
        )
        if minion:
            player.DealEncounterCard(minion, effect)

    return [
        AbilityFactory.FirstCardPlayerRevealsGain(
            CardFinder(name="Hound"),
            "AnyPlayer",
            each_phase_round="Phase",
            gain_surge=1
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            release_the_hounds,
            has_defeating_player=True
        ),
    ]

