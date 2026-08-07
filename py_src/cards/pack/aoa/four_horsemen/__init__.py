from cards.pack import *

def GetNextVillain(effect: 'Effect', villain: 'Villain|None') -> 'Villain':
    # villain = Worlds.FindVillain(effect)
    villains = Worlds.GetVillains(effect)
    # villains = [x for x in villains if x.health > 0]

    if villain is None:
        return villains[0]

    index = villains.index(villain)
    return villains[(index + 1) % len(villains)]

def HorsemanOf(name: str) -> List['Ability']:
    
    def horseman_of_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name=name,
            card_type=Enemy
        )
        if villain:
            this.HealthUnits([villain], 2, effect)
            Faces.GiveStatus([villain], "Tough", effect)
            villain.DoActivate(player, effect)

    def horseman_of_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            villain = Worlds.FindCardOnField(
                effect,
                name=name,
                card_type=Enemy
            )
            if villain:
                villain.DoActivate(player, effect, do_not_give_boost=True)

        message.AfterThisActivation(effect, action)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            horseman_of_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            horseman_of_boost
        ),
    ]

def ThisCannotBeDefeatedWhileAnotherVillainHas1HP() -> List['Ability']:
    return AbilityFactory.UnitCannotBeDefeatedWhile(
        AbilityType.NonKeywordBold,
        "This",
        while_card_is_in_play=CardFinder(card_type=Villain, has_non_health=False),
        change_on_event=OnEvent.Health(Villain)
    )

def TreatAsIfBlankUntilNextVillainPhase(player: 'Player', effect: 'Effect'):
    identity = player.GetIdentity()
    identity.TreatAsIfBlank(effect, None, until_next_villain_phase_begin=True)

