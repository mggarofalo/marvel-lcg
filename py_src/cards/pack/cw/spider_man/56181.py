from . import *

# Tangled Up

def GetAbilities() -> Sequence['Ability']:

    def tangled_up(effect: 'Effect', message: 'Message.WhenUnitWouldScheme|Message.WhenUnitWouldAttack|Message.WhenUnitWouldThwart|Message.WhenPlayerWouldPlayCard') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.DiscardAll([this], effect)

    def tangled_up_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        if Worlds.FindCardOnField(
            effect,
            name="Spider-Man",
            card_type=Minion
        ):
            this.Reveal(player, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourLeader",
            otherwise_attach_to="YourHero"
        ),
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            tangled_up
        ),
        *AbilityFactory.WhenAttachedPlayerWouldThwart(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            tangled_up
        ),
        *AbilityFactory.WhenAttachedPlayerWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            tangled_up
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tangled_up_boost
        ),
    ]

