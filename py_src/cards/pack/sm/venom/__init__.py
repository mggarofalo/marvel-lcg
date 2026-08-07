from cards.pack import *

def PlaceFaceDownBoostCardsOnPlayer(player: 'Player', size: int, by_effect: 'Effect'):
    for _ in range(size):
        face = Worlds.PopEncounterCard(by_effect)
        player.GetIdentity().PlaceCardHere(face, False, by_effect)


def VengeanceRetribution(level: int):

    def venom(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.attacker.GetControlByPlayer()
        if player:
            num = 0
            if level == 1 or level == 2:
                num = 1
            if level == 3:
                num = 1
                if len(effect.world.stat.this_turn_made_attack) == 1 and \
                    effect.world.stat.this_turn_made_attack[0][1] == message.by_effect:
                        num = 2

            PlaceFaceDownBoostCardsOnPlayer(player, num, effect)

    name = "Vengeance"
    if level == 3:
        name = "Retribution"

    # Fix "33009"
    return AbilityFactory.AfterUnitAttackEnd(
        AbilityType.ForcedResponse,
        CardFinder(card_type=Identity|Ally),
        venom,
        damaged_who="This",
        # conditions=[
        #     lambda effect, message:
        #         message.by_effect.initiator.IsPlayer()
        # ],
    ).SetName(name)

def AfterVenomTakesAttackDamageRemoveEqualThreat():
    def lashing_out(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = message.took_damage
        this.RemoveThreatFromSchemes([this], value, effect)

    return AbilityFactory.AfterUnitTookDamage(
        AbilityType.Response,
        CardFinder(name="Venom", card_type=Villain),
        lashing_out,
        is_from_attack=True,
    )

def FindBellTower(by_effect: 'Effect') -> 'Environment|None':
    face = Worlds.FindCardOnField(by_effect, name="Bell Tower", card_type=Environment)
    return face


