from . import *

# Power of the Atom

def GetAbilities() -> Sequence['Ability']:

    def power_of_the_atom_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            ThisCardGainSurge(effect)

        face = Find.FindAndReveal(
            effect,
            player,
            name="Radioactive Man",
            card_type=Enemy
        )
        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def power_of_the_atom_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        identity.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            power_of_the_atom_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            power_of_the_atom_boost
        ),
    ]

