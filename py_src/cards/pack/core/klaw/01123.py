from . import *

# Sonic Boom

def GetAbilities() -> Sequence['Ability']:

    def sonic_boom_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def exhaust_each_characters(targets: Sequence['CardFace']) -> None:
            Faces.ExhaustAll(targets, effect)

        player.ChooseAbilities(
            effect,
            # "You must choose an option that you can fulfill. If you cannot
            # fulfill either option, then you must do as much as you can, which
            # typically means discarding one or two different resource icons
            # from your hand." -- which is what `ForChoiceAbilityToSpend` says
            # here and `ChooseAbilitiesHelper` carries out (MARVEL-109).
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("YBR"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Exhaust each character you control",
                exhaust_each_characters
            ).SetTarget("YouControlUnit", range="All", canbe_exhaust=True)
        )

    def sonic_boom_boost(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        # if message.deal_damage > 0 and not message.trigger.IsDefeated():
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.trigger.GetControlByPlayer()
        if player.IsHero():
            Faces.ExhaustAll([player.GetHero()], effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sonic_boom_revealed
        ),
        AbilityFactory.BoostIfThisActivationDealsDamage(
            "You",
            sonic_boom_boost,
        )
    ]
