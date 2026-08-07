from . import *

# Land Out of Time

def GetAbilities() -> Sequence['Ability']:

    def land_out_of_time_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        
        face = Worlds.FindCardOnField(
            effect,
            name="The Savage Land",
        )
        if face:
            faces: List['CardFace'] = []
            def action(effect: 'Effect', message: 'Message.AfterCardDiscard'):
                faces.append(message.trigger)

            temp_effects = this.effect.RegisterTemp(
                AbilityFactory.AfterCardDiscardInternal(
                    AbilityType.Temp0,
                    None,
                    action,
                    conditions=[
                        lambda check_effect, check_message:
                            check_message.by_effect.this == face
                    ]
                ),
                unregister_after_exec=False
            )
            face.ResolveSpecialAbility(player, effect)

            Effects.UnRegister(temp_effects)

            damage = FacesCounter.GetPrintedResourcesIcon(faces, player.player_deck)
            player.GetIdentity().TakeIndirectDamage(this, damage, effect)
        else:
            Find.FindAndReveal(
                effect,
                player,
                name="The Savage Land",
            )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            land_out_of_time_revealed
        ),
    ]

