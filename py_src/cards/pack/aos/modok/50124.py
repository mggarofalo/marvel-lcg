from . import *

# Psionic Blast

def GetAbilities() -> Sequence['Ability']:

    def psionic_blast_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Confused", effect)
        face = Worlds.FindCardOnField(effect, MODOK_FINDER, card_type=Enemy)
        if face:
            face.DoSchemes(player, effect)

    def psionic_blast_revealed_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        face = Worlds.FindCardOnField(effect, MODOK_FINDER, card_type=Enemy)
        if face:
            damage = face.scheme
        else:
            damage = 0

        def action(check_effect: 'Effect', check_message: 'Message.AfterUnitTookDamage'):
            Faces.GiveStatus([check_message.trigger], "Confused", effect)

        effects = this.effect.RegisterTemp(
            AbilityFactory.AfterUnitTookDamage(
                AbilityType.Temp0,
                None,
                action,
                conditions=[
                    lambda check_effect, check_message:
                        effect == check_message.by_effect
                ]
            ),
            unregister_after_exec=False,
        )

        identity.TakeIndirectDamage(this, damage, effect)

        Effects.UnRegister(effects)

    return [
        AbilityFactory.WhenThisRevealed(
            "Hero",
            psionic_blast_revealed_hero
        ),
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            psionic_blast_alter_ego
        ),
    ]

