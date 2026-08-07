from . import *

# Special Delivery

def GetAbilities() -> Sequence['Ability']:

    def you_may_exhaust_milano(player: 'Player', effect: 'Effect') -> bool:
        exhaust_effect = player.MayChooseOneAbility(
            effect,
            ExhaustTheMilanoForChoiceAbility(effect),
        )
        return exhaust_effect != None

    def special_delivery_reveal_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if not you_may_exhaust_milano(player, effect):
            villain = Worlds.FindVillain(effect)
            if villain:
                villain.DoSchemes(player, effect, property=SchemeProperty(additional_value=1))


    def special_delivery_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if not you_may_exhaust_milano(player, effect):
            villain = Worlds.FindVillain(effect)
            if villain:
                villain.DoAttackYou(player, effect, property=AttackProperty(additional_value=1))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            special_delivery_reveal_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            special_delivery_reveal_hero
        ),
    ]

