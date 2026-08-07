from . import *

# Spell Blast

def GetAbilities() -> Sequence['Ability']:

    def spell_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not identity.HasTrait("DEFIANT") and not identity.HasTrait("ENTHRALLED"):
            return

        def action_scheme(targets: Sequence['CardFace']):
            enchantress = Worlds.FindCardOnField(effect, name="Enchantress", card_type=Enemy)
            if enchantress:
                value = 1 if Worlds.IsExpert(effect) else 0
                enchantress.DoSchemes(player, effect, property=SchemeProperty(additional_value=value))

        def action_attack(targets: Sequence['CardFace']):
            enchantress = Worlds.FindCardOnField(effect, name="Enchantress", card_type=Enemy)
            if enchantress:
                value = 1 if Worlds.IsExpert(effect) else 0
                enchantress.DoAttackYou(player, effect, property=AttackProperty(additional_value=value))

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Enchantress schemes",
                action_scheme
            ),
            AbilityFactory.ForChoiceAbility(
                "Enchantress attacks you",
                action_attack
            ),
            choose_x_different_options=2 if identity.HasTrait("ENTHRALLED") else 1
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spell_blast_revealed
        ),
    ]

