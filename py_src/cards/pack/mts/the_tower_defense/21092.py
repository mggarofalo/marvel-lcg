from . import *

# * Proxima Midnight

def GetAbilities() -> Sequence['Ability']:

    def proxima_midnight(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.property.against_player
        if player:
            player.ChooseAbilities(
                effect,
                AbilityDealDamageToAvengersTower(1, effect),
                AbilityFactory.ForChoiceAbility(
                    "Proxima Midnight gets +2 ATK for this attack",
                    lambda targets:
                        message.GainATKForThisAttack(+2, effect)
                ),
            )

    def get_main_scheme(effect: 'Effect', message: 'Message.GettingMainScheme') -> 'MainScheme|None':
        for villain in Worlds.GetVillains(effect):
            if villain.IsActive():
                scheme = villain.FindCorrespondingScheme()
                if MainScheme.IsType(scheme):
                    return scheme
        return None

    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            proxima_midnight,
        ),
        *AbilityFactory.UnitCannotBeDefeatedWhile(
            AbilityType.NonKeywordBold,
            "This",
            condition=lambda effect:
                CheckVillainHealth("Corvus Glaive", effect) > 0,
            change_on_event=OnEvent.Health(Villain),
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.NonKeyword,
            CardFinder(name="Corvus Glaive"),
            lambda effect, message:
                effect.this.CastTo(EncounterVillain).Advance(effect)
        ),
        AbilityFactory.GetMainScheme(
            get_main_scheme,
        ),
    ]

