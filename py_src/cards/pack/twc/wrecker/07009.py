from . import *

# Escaped Convict

def GetAbilities() -> Sequence['Ability']:

    def escaped_convict(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = GetSideSchemeWhoseHas(effect, least_threat=True)
        villain = scheme.FindCorrespondingVillain()
        if villain:
            villain.SetActive(effect)

            player = message.GetToPlayer()
            if player.IsHero() and message.being_message.IsAttack():

                def villain_attacks_you():
                    villain.DoAttackYou(player, effect, property=AttackProperty(do_not_give_boost=True))

                message.AfterThisActivation(effect, villain_attacks_you)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            escaped_convict,
        )
    ]

