from cards.pack import *
from cards.pack.gmw import *

DRANGS_SPEAR = "Drang's Spear"

def ResolveTheBadoonShipChargeUpAbility(effect: 'Effect', message: 'Message2'):
    face = Worlds.FindCardOnField(
        effect,
        name="Badoon Ship",
        card_type=Environment
    )
    player = Worlds.GetCurrentPlayer(effect)
    assert face
    face.ResolveSpecialAbility(player, effect, name="Charge Up")

def ResolveBadoonShipChargeUpabilityAfterThisScheme() -> 'Ability':

    return AbilityFactory.AfterUnitSchemeEnd(
        AbilityType.ForcedResponse,
        "This",
        ResolveTheBadoonShipChargeUpAbility
    )

def ResolveBadoonShipChargeUpabilityAfterThisActive() -> 'Ability':

    return AbilityFactory.AfterEnemyActivationEnd(
        AbilityType.ForcedResponse,
        "This",
        ResolveTheBadoonShipChargeUpAbility
    )

