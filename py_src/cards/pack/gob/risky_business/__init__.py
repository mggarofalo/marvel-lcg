from cards.pack import *

def WhenRevealDealDamangeToPlayers(damange: int, only_hero_form: bool, indirect: bool) -> 'Ability':

    def deal_indirect_damage_to(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)

        def action(player: 'Player'):
            unit = None
            if only_hero_form:
                if player.IsHero():
                    unit = player.GetHero()
            else:
                unit = player.GetIdentity()
            if unit:
                if indirect:
                    this.DealIndirectDamage(unit, damange, effect)
                else:
                    this.DealDamage([unit], damange, effect)

        Players.ForEachPlayer(effect, action)


    return AbilityFactory.WhenThisRevealed(
        None,
        deal_indirect_damage_to
    )

def RemoveMadnessCounterInsteadOfScheme(num: int) -> 'Ability':

    def remove_madness_counter(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        message.SetBeInstead(effect)
        StateofMadnessPlaceMadness(-1 * num, effect)

    return AbilityFactory.WhenUnitWouldScheme(
        AbilityType.ForcedInterrupt,
        "This",
        remove_madness_counter
    )

def PlaceInfamyCounterInsteadAttack(num: int) -> 'Ability':
    def place_infamy_counter_instead(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        message.SetBeInstead(effect)
        CriminalEnterprisePlaceInfamy(+1 * num, effect)

    return AbilityFactory.WhenUnitWouldAttack(
        AbilityType.ForcedInterrupt,
        "This",
        place_infamy_counter_instead
    )

def RemoveInfamyCounterInsteadTakeDamange() -> 'Ability':
    def remove_infamy_counter_instead(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        message.SetBeInstead(effect)
        CriminalEnterprisePlaceInfamy(-message.will_take_damage, effect)

    return AbilityFactory.WhenUnitWouldTakeDamage(
        AbilityType.ForcedInterrupt,
        "This",
        remove_infamy_counter_instead
    )

################################################################################
#
def CriminalEnterprisePlaceInfamy(counters: int|Literal["1*"], by_effect: 'Effect'):
    this = by_effect.this
    Unused(this)

    counters = Worlds.ConvertPerPlayerIconToInt(counters, by_effect)

    face = Worlds.FindCardOnField(by_effect, name='Criminal Enterprise', card_type=Environment)
    if face:
        assert face, f"{this=}"
        if counters > 0:
            Faces.PlaceCountersOn([face], counters, 'infamy', by_effect)
        else:
            counters *= -1
            Faces.RemoveCountersOn([face], counters, 'infamy', by_effect)

def CriminalEnterpriseGetInfamyCounters(by_effect: 'Effect') -> int:
    card = Worlds.FindCardOnField(by_effect, name='Criminal Enterprise', card_type=Environment)
    if card:
        return card.GetCounters('infamy')
    else:
        return 0

def StateofMadnessPlaceMadness(counters: int, by_effect: 'Effect'):
    this = by_effect.this
    Unused(this)
    face = Worlds.FindCardOnField(by_effect, name='State of Madness', card_type=Environment)
    if face:
        if counters > 0:
            Faces.PlaceCountersOn([face], counters, 'madness', by_effect)
        else:
            counters *= -1
            Faces.RemoveCountersOn([face], counters, 'madness', by_effect)

def StateofMadnessGetMadnessCounters(by_effect: 'Effect') -> int:
    card = Worlds.FindCardOnField(by_effect, name='State of Madness', card_type=Environment)
    if card:
        return card.GetCounters('madness')
    else:
        return 0

def CriminalEnterpriseExist(by_effect: 'Effect') -> bool:
    card = Worlds.FindCardOnField(by_effect, name="Criminal Enterprise", card_type=Environment)
    return card != None

def VillainIsNormanOsborn(by_effect: 'Effect', message: 'Message2') -> bool:
    villain = Worlds.FindVillain(by_effect)
    if villain:
        return villain.IsName("Norman Osborn")
    else:
        return False

def VillainIsGreenGoblin(by_effect: 'Effect', message: 'Message2') -> bool:
    villain = Worlds.FindVillain(by_effect)
    if villain:
        return villain.IsName("Green Goblin")
    else:
        return False

def BoostPlaceInfamyOrRemoveMadness() -> 'Ability':

    def private_security_specialist(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this
        Unused(this)

        if CriminalEnterpriseExist(effect):
            CriminalEnterprisePlaceInfamy(+1, effect)
        else:
            StateofMadnessPlaceMadness(-1, effect)

    return AbilityFactory.WhenCardBecomeBoost(
        "This",
        private_security_specialist
    )

