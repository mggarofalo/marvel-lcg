from . import *

# The Search for Spiral

def GetAbilities() -> Sequence['Ability']:

    def the_search_for_spiral_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = effect.GetInitiator()
        identity = player.GetIdentity()
        identity.TakeDamage(this, 2, effect)
        this.RemoveThreatFromSchemes([this], 3, effect)

    def the_search_for_spiral(effect: 'Effect', message: 'Message.AfterSchemeRemoveThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetByPlayer()

        if player:
            face = GetShowDeck(effect).GetTopCard()
            if face:
                face.Reveal(player, effect)

        this.PlaceThreatOnSchemes([this], "3*", effect)


    return [
        AbilityFactory.AfterSchemeRemoveThreat(
            AbilityType.ForcedResponse,
            "This",
            the_search_for_spiral,
            last_threat=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            the_search_for_spiral_action,
        ),
    ]

