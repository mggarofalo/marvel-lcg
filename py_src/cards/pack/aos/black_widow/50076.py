from . import *

# Attacrobatics

def GetAbilities() -> Sequence['Ability']:

    def attacrobatics_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if player.IsAlterEgo():
            player.GetIdentity().ChangeToForm(Hero, effect)

        villain = Worlds.FindVillain(effect, name="Black Widow")
        if villain:
            villain.DoAttackYou(player, effect, property=AttackProperty(additional_boost_card=1))

    def attacrobatics_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.PreventAllDamage(effect)

        if Worlds.IsExpert(effect):
            if message.attacker:
                damage = message.damage
                this.DealDamage([message.attacker], damage, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            attacrobatics_revealed
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            attacrobatics_preparation
        ),
    ]

