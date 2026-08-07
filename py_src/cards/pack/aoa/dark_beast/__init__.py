from cards.pack import *

def ResolveSpecialAbilityOnTheSETTINGEnvironment(player: 'Player|None', effect: 'Effect'):
    env = Worlds.FindCardOnField(
        effect,
        trait="SETTING",
        card_type=Environment
    )
    if env:
        env.ResolveSpecialAbility(player, effect)

def WhenDarkBeastAttacksYouResolveSpecialAbilityOnTheSETTINGEnvironment() -> 'Ability':
    
    def dark_beast(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)

    return AbilityFactory.WhenUnitAttackYou(
        AbilityType.ForcedInterrupt,
        "This",
        dark_beast
    )

def RevealRandomSetAsideEnvironmentAndShuffleRest(effect: 'Effect'):
    envs = Worlds.AsideDeck(effect).FindCards(card_type=Environment)
    if envs:
        env = Rand.RandomChoice(envs, effect)
        env.Reveal(None, effect)

        faces = Worlds.AsideDeck(effect).FindCards(set_name=env.paper.set_name)
        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)

