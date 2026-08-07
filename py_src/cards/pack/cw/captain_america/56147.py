from . import *

# Shield Toss

def GetAbilities() -> Sequence['Ability']:

    def shield_toss_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        cap = Worlds.FindCardOnField(effect, name="Captain America", card_type=Leader)
        if cap:
            cap.DoSchemes(player, effect)

    def shield_toss_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        shield = Find.Find(
            effect,
            name="Cap's Shield",
            card_type=Attachment,
        )
        if shield:
            cap = Worlds.FindCardOnField(effect, name="Captain America", card_type=Leader)
            if cap:
                if shield.bind_face == cap:
                    this.DealDamage(player.GetControlCharacters(), 2, effect)
                else:
                    shield.AttachTo2(cap, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            shield_toss_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            shield_toss_hero
        ),
    ]

