from . import *

# Self-Experimentation

def GetAbilities() -> Sequence['Ability']:

    def self_experimentation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Mister Hyde",
            card_type=Minion,
        )
        if face:
            face.Reveal(player, effect)


    def self_experimentation(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.SetBeInstead(effect)

        value = message.will_take_damage
        this.RemoveThreatFromSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            self_experimentation_revealed
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            CardFinder2("BRUTE", Enemy),
            self_experimentation,
        )
    ]

