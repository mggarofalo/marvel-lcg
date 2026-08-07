from . import *

# * Infinity Gauntlet

def GetAbilities() -> Sequence['Ability']:

    def infinity_gauntlet(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()

        faces = Worlds.FindCardsOnField(
            effect,
            trait="INFINITY STONE",
            card_type=Environment
        )
        if faces:
            for face in faces:
                face.ResolveSpecialAbility(player, effect, sequence=faces)
        else:
            face = GetInfinityStone(effect).GetTopCard()
            if face:
                face.PutIntoPlay(player, effect)

    def infinity_gauntlet_begin(villain: 'CardFace', effect: 'Effect') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        deck = GetInfinityStone(effect)

        if not deck.initialize:
            assert Villain.IsType(villain)
            deck.Create(effect, villain)

            faces = Worlds.GetEncounterDeckCards(effect) + Worlds.GetEncounterDiscardPileCards(effect)
            faces = CardFinder(set_name=this.paper.set_name).Checks(faces)
            deck.PushCards(faces, effect)

    return [
        # When using the Infinity Gauntlet set in a scenario, attach the Infinity Gauntlet attachment card to the villain during setup. If there is more than one villain (or no villain) in play at the start of the game, The Infinity Gauntlet set cannot be used
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain,
            when_attach_operation=infinity_gauntlet_begin
        ),
        # After attaching the Infinity Gauntlet to the villain, shuffle the six Infinity Stone environment cards together and set them aside, facedown. This is the “Infinity Stone deck.” The Infinity Stone deck has its own discard pile. When an Infinity Stone environment is discarded, it is placed in the Infinity Stone deck discard pile. If the Infinity Stone deck is ever empty, shuffle the Infinity Stone deck discard pile back into the Infinity Stone deck. There is no built-in penalty for doing this.
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "AttachedVillain",
            infinity_gauntlet,
        ),
    ]

