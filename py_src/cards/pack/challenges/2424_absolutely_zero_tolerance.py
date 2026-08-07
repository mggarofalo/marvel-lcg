from . import *

# Absolutely Zero Tolerance

def GetAbilities() -> Sequence['Ability']:

    def absolutely_zero_tolerance(effect: 'Effect', message: 'Message.WhenGameWouldBegin|Message.WhenPhaseEnd'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        face = Search.EncounterCard(
            effect,
            include_discard_pile=False,
            name="Bastion",
        )
        if face:
            first_player.DealEncounterCard(face, effect)

    def absolutely_zero_tolerance_find(effect: 'Effect', message: 'Message.WhenGameWouldBegin|Message.WhenPhaseEnd'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        face = Search.SearchForCard(
            effect,
            first_player,
            include_encounter_discard_pile=True,
            include_victory_display=True,
            name="Bastion",
        )
        if face:
            first_player.DealEncounterCard(face, effect)

    return [
        # AbilityFactory.UsingOnlyHeroes(
        #     X-Men
        # ),
        AbilityFactory.AfterGameSetup(
            absolutely_zero_tolerance
        ),
        AbilityFactory.WhenVillainPhaseEnd(
            AbilityType.Challenge,
            absolutely_zero_tolerance_find,
            # condition=lambda effect, message:
            #     Worlds.GetEncounterDiscardPile(effect).FindCardSize(CardFinder(name="Bastion")) > 0 or \
            #     Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(name="Bastion")) > 0
        ),
    ]

