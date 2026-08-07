from . import *

# Seek the Weak

def GetAbilities() -> Sequence['Ability']:

    def seek_the_weak_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)
        if villain:
            size = len(GetVillainsUnderRouted(effect))
            if size >= 1:
                overkill = True
            else:
                overkill = False
            villain.DoAttackYou(player, effect, property=AttackProperty(overkill=overkill))

    def seek_the_weak_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.would_atk_message.GainOverKill(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            seek_the_weak_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            seek_the_weak_boost,
            during_attack=True
        ),
    ]

