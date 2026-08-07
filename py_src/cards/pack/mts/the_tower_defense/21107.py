from . import *

# Corvus's Cunning

def GetAbilities() -> Sequence['Ability']:

    def corvuss_cunning_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindCardOnField(
            effect,
            name="Corvus Glaive",
            card_type=Villain
        )
        if villain:
            villain.DoActivate(player, effect)

    def corvuss_cunning_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = message.being_message.by_face.CastTo(Enemy)
        atk = 0
        sch = 0
        if Villain.IsType(face):
            for villain in Worlds.GetVillains(effect):
                if villain != face:
                    atk += villain.attack
                    sch += villain.scheme
        face.GainForThisActive(effect, message.being_message, attack=atk, scheme=sch)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            corvuss_cunning_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            corvuss_cunning_boost
        ),
    ]

