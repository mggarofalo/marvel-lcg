from . import *

# Venom Blast

def GetAbilities() -> Sequence['Ability']:

    def venom_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Spider-Woman",
            card_type=Enemy
        )
        if face:
            def action(remove_effect: 'Effect', remove_message: 'Message.AfterCardRemovedCounter'):
                size = remove_message.removed_counters
                attack_message = remove_message.by_effect.GetBindMessage(Message.WhenUnitWouldAttack)
                if size >= 1:
                    attack_message.GainRanged(effect)
                if size >= 2:
                    attack_message.GainPiercing(effect)
                if size >= 3:
                    attack_message.GainOverKill(effect)
            effects = this.effect.RegisterTemp(
                AbilityFactory.AfterCardRemovedCounter(
                    AbilityType.Temp0,
                    FindFinesse(effect),
                    'skill',
                    action,
                ),
                unregister_after_exec=False
            )
            face.DoAttackYou(player, effect)
            Effects.UnRegister(effects)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            venom_blast_revealed
        ),
    ]

