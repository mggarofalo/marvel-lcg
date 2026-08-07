from . import *

# Magistrate

def GetAbilities() -> Sequence['Ability']:

    def magistrate(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            face = Find.Find(
                effect,
                name="Escaped Mutant",
                card_type=Attachment
            )
            if face:
                identity = player.GetIdentity()
                face.AttachTo2(identity, effect)

    def magistrate_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.GetInventoryDeck().FindCard(name="Escaped Mutant"):
            message.GainBoostIcon(3, effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            magistrate
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            magistrate_boost
        ),
    ]

