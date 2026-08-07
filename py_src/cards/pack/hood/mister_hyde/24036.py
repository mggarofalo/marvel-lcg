from . import *

# Hyde Formula

def GetAbilities() -> Sequence['Ability']:

    def hyde_formula_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        zabo = Worlds.FindCardOnField(
            effect,
            name="Calvin Zabo",
            card_type=Minion
        )
        hyde = Worlds.FindCardOnField(
            effect,
            name="Mister Hyde",
            card_type=Minion
        )

        if zabo:
            zabo.DoSchemes(player, effect, property=SchemeProperty(additional_value=3))
            zabo.TakeDamage(this, 4, effect)
        if hyde:
            Faces.GiveStatus([hyde], "Tough", effect)
            hyde.DoAttackYou(player, effect)
        if not zabo and not hyde:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hyde_formula_revealed
        ),
    ]

