from . import *

# Hydra Jet-Trooper

def GetAbilities() -> Sequence['Ability']:

    def hydra_jet_trooper_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            message.AfterThisActivation(
                effect,
                lambda: villain.DoAttackYou(player, effect, property=AttackProperty(do_not_give_boost=True))
            )


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hydra_jet_trooper_boost,
            you_are_in_hero_from=True,
        ),
    ]

