from . import *

# Mimicry

def GetAbilities() -> Sequence['Ability']:

    def mimicry_reveal_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        faces = player.DiscardDeckTopCards(5, effect)
        for face in faces:
            if face.HasTrait('THWART') and villain:
                villain.DoSchemes(player, effect)
                break

    def mimicry_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        faces = player.DiscardDeckTopCards(5, effect)
        for face in faces:
            if face.HasTrait('ATTACK') and villain:
                villain.DoAttackYou(player, effect)
                break


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            mimicry_reveal_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            mimicry_reveal_hero
        ),
    ]

