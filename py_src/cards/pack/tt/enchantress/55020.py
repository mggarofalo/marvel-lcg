from . import *

# * Ulik

def GetAbilities() -> Sequence['Ability']:

    def ulik(effect: 'Effect', message: 'Message.AfterFaceDealDamage') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        attacker = message.attacker
        if attacker:
            player = attacker.GetControlByPlayer()
            this.DoAttackYou(player, effect)

    def ulik_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetDefeatingPlayer()
        face = Worlds.FindCardOnField(
            effect,
            name="Enchantress",
            card_type=Enemy
        )
        if face:
            face.DoAttackYou(player, effect, property=AttackProperty(additional_value=1))


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                Worlds.IsExpert(effect),
            toughness=1
        ),
        AbilityFactory.AfterYouDealDamage(
            AbilityType.ForcedResponse,
            CardFinder(name="Enchantress"),
            ulik,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            ulik_defeated
        ),
    ]

