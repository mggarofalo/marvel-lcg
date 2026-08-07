from . import *

# * Shocker

def GetAbilities() -> Sequence['Ability']:

    def shocker(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([message.attacker], "Stunned", effect)


    def shocker_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        unit = Filter.One(player.GetControlCharacters(), effect, highest_atk=True)
        if unit:
            Faces.GiveStatus([unit], "Stunned", effect)


    return [
        AbilityFactory.AfterUnitBeAttacked(
            AbilityType.ForcedResponse,
            "This",
            shocker
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            shocker_boost
        ),
    ]

