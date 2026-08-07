from . import *

# Heroic Strike

def GetAbilities() -> Sequence['Ability']:

    def heroic_strike_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, ally=True, upgrade=True)

    def heroic_strike_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        cap = Worlds.FindCardOnField(effect, name="Captain America", card_type=Leader)
        if cap:
            def action(units: Sequence[Unit2]):
                Faces.GiveStatus(units, "Stunned", effect)
            cap.DoAttackYou(player, effect, if_characters_damaged_by_this=action)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            heroic_strike_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            heroic_strike_hero
        ),
    ]

