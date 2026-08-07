from . import *

# Battle for Limbo

def GetAbilities() -> Sequence['Ability']:

    def battle_for_limbo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            ThisCardGainSurge(effect)

        Worlds.Enemies.AllMinionActivateAgainstEngagedPlayer(effect, trait="LIMBO", if_no_activate=action)


    def battle_for_limbo_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="Ruler of Limbo",
            card_type=SchemeSide2
        )

        if face:
            this.PlaceThreatOnSchemes([face], 2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            battle_for_limbo_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            battle_for_limbo_boost
        ),
    ]

