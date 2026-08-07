from cards.pack import *

# Wicked Ambitions

def GetAbilities() -> Sequence['Ability']:

    def wicked_ambitions(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        value = Worlds.GetVillainStage(effect) * 2
        identity = player.GetIdentity()

        def choose(face: 'CardFace'):
            if face.HasTrait('GOBLIN') and Minion.IsType(face):
                minion = face.CastTo(Minion)

                def take_3_damage(targets: Sequence['CardFace']):
                    identity.TakeDamage(this, 3, effect)

                def engaged_minion(targets: Sequence['CardFace']):
                    minion.PutIntoPlay(player, effect)

                player.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Take 3 damage",
                        take_3_damage
                    ).SetTarget("YourIdentity"),
                    AbilityFactory.ForChoiceAbility(
                        "Put that minion into play engaged with you",
                        engaged_minion
                    )
                )

        Worlds.DiscardEncounterCards(value, effect, each_time=choose)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            wicked_ambitions
        ),
    ]

