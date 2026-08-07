from cards.pack import *

SURGING_SANDS = "Surging Sands"

def GetCityStreets(effect: 'Effect') -> 'Environment|None':
    return Worlds.FindCardOnField(
        effect,
        name="City Streets",
        card_type=Environment
    )


def ResolveSurgingSandsOnCityStreets(effect: 'Effect'):
    city_streets = GetCityStreets(effect)
    if city_streets:
        first_player = Worlds.GetFirstPlayer(effect)
        city_streets.ResolveSpecialAbility(first_player, effect, name=SURGING_SANDS)


def SandBlastWave(level: int) -> Ability:

    def sand_blast_wave(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        if level == 3:
            message.GainOverKill(effect)
        else:
            message.SetDealIndirectDamage(effect)

        def action(units: List['Unit2']):
            ResolveSurgingSandsOnCityStreets(effect)

        message.IfUnitTakeDamageFromThisAttack(action, effect, which_unit=Identity)


    ability = AbilityFactory.WhenUnitAttackYou(
        AbilityType.ForcedInterrupt,
        "This",
        sand_blast_wave,
    )

    if level == 3:
        ability.SetName("Sand Wave")
    else:
        ability.SetName("Sand Blast")

    return ability

