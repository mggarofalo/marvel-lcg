from . import *

# Apprehending Rogue Agents

def GetAbilities() -> Sequence['Ability']:

    def apprehending_rogue_agents(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.attacker.GetControlByPlayer()
        message.attacked.CastTo(Minion).EngagePlayer(player, effect)

    def apprehending_rogue_agents_condition(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> bool:
        player = message.attacker.GetControlByPlayer()
        minion = message.attacked.CastTo(Minion)
        minion.IsDefeated()
        return not minion.IsDefeated() and minion.engaged_player != player

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("THUNDERBOLT", Minion),
            guard=1,
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            Identity,
            CardFinder2("THUNDERBOLT", Minion),
            apprehending_rogue_agents,
            conditions=[
                apprehending_rogue_agents_condition
            ]
        ),
    ]

