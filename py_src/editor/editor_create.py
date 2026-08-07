from core import *
from engine.lib import Json
from engine.file import FileManager
from engine.config import ConfigVariables

CARDS_JSON_FILE = ConfigVariables.File('cards_json_file')

def DoEditor(data: Dict[str, List[str]]):
    # from cards.paper import Paper
    # from editor.editor import Editor
    # editor = Editor.Get()

    @dataclass
    class Data:
        card_id: str
        type: str
        name: str
        subtitle: str
        unique: bool
        traits: List[str]
        pack: str
        set_name: str
        text: str
        ability_link: str
        full_link: str

    def Load(data: Dict[str, List[str]]) -> 'Data':
        return Data(
            data['card_id'][0].strip(),
            data['type'][0].strip(),
            data['name'][0].strip(),
            data['subtitle'][0].strip() if 'subtitle' in data else '',
            True if 'unique' in data and data['unique'][0].strip() == "1" else False,
            [x.rstrip(".") for x in data['traits'][0].strip().split(". ")] if 'traits' in data else [],
            data['pack'][0].strip(),
            data['set_name'][0] if 'set_name' in data else '',
            data['text'][0] if 'text' in data else '',
            data['ability_link'][0] if 'ability_link' in data else '',
            data['full_link'][0] if 'full_link' in data else '',
        )

    def Save(obj: 'Data', card_name: str, desc: Dict[str, str]):
        card_datas = Json.Load(CARDS_JSON_FILE.value, check_sum="Warn")

        if obj.pack not in card_datas:
            card_datas[obj.pack] = []

        if obj.full_link:
            new_card = {"card_id": obj.card_id, "full_link": obj.full_link}
        elif obj.ability_link:
            new_card = {"card_id": obj.card_id, "type": obj.type, "name": card_name, "subtitle": obj.subtitle, "desc": desc, "traits": obj.traits, "set_name": obj.set_name, "text": obj.text, "ability_link": obj.ability_link}
        else:
            new_card = {"card_id": obj.card_id, "type": obj.type, "name": card_name, "subtitle": obj.subtitle, "desc": desc, "traits": obj.traits, "set_name": obj.set_name, "text": obj.text}

        # Check if card_id already exists and replace it
        pack_cards = card_datas[obj.pack]
        for i, card in enumerate(pack_cards):
            if card['card_id'] == new_card['card_id']:
                pack_cards[i] = new_card
                break
        else:
            pack_cards.append(new_card)

        for key in card_datas:
            card_datas[key] = sorted(card_datas[key], key=lambda d: d['card_id'])

        Json.Save(card_datas, CARDS_JSON_FILE.value, ignore_check_sum=False)


    obj = Load(data)

    if obj.full_link != "":
        Save(obj, "", {})
        return

    # Log.Print(obj)

    desc: Dict[str, str] = {}
    from game.card.face.card_face import CardFace
    for e in Types.LiteralToList(CardFace.KEY):
        if e in data:
            desc[e] = data[e][0]

    card_name = f"{'* ' if obj.unique else ''}{obj.name}"
    action_name = FileManager.CleanName(card_name)
    subtitle = obj.subtitle
    if subtitle:
        subtitle = ': ' + subtitle
    card_type = obj.type

    if card_type == "SideScheme":
        card_type = "EncounterSideScheme"

    def AddEffect():

        # Add effect
        boost_func = ""
        boost_ability = ""
        attach_ability = ""
        turn_ability = ""
        turn_func = ""
        reveal_ability = ""
        reveal_func = ""
        obligation_func = ""
        obligation_ability = ""
        asset_func = ""
        asset_ability = ""
        env_func = ""
        env_ability = ""
        defeated_func = ""
        defeated_ability = ""
        evidence_func = ""
        evidence_ability = ""

        if card_type == "Resource":
            turn_func = f"""    def {action_name}(effect: 'Effect', message: 'Message.AfterCardBeSpendAsResource') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

        initiator = effect.GetInitiator()


    def {action_name}(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> 'Resources':
        this = effect.this.CastTo({card_type})
        Unused(this)

        res = Faces2.GetPrintedResources([this])
        if message.paying_card != None and message.paying_card.IsClass(""):
            res *= 2
        return res
"""

            turn_ability = f"""
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.,
            {action_name}
        ),
        AbilityFactory.CanDiscardToGainResources(
            {action_name}
        ),"""

        if card_type in [ "MainScheme", "EncounterSideScheme", "PlayerSideScheme"]:
            defeated_func = f"""    def {action_name}_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

        player = message.GetDefeatingPlayer()

        def action(player: 'Player'):
            player.

        Players.ForEachPlayer(effect, action)

"""
            defeated_ability = f"""
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            {action_name}_defeated,
        ),"""

        if card_type in [ "MainScheme", "EncounterSideScheme", "PlayerSideScheme", "Environment"]:
    #         env_func = f"""    def {action_name}_env(effect: 'Effect', unit: 'CardFace', diff: int) -> None:
    #         this = effect.this.CastTo({card_type})
    #         Unused(this)

    # """
            env_ability = f"""
        # *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
        #     "Character",
        #     change_on_event=
        # ),"""


        if card_type in [ "Minion", "EncounterSideScheme", "Attachment", "Environment", "Treachery"]:
            boost_func = f"""    def {action_name}_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

"""

            boost_ability = f"""
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            {action_name}_boost
        ),"""


        if card_type == "Attachment":
            turn_func = f"""    def {action_name}(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

"""
            attach_ability = f"""
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
            # CardFinder(card_type=Villain)
        ),
        # AbilityFactory.UnitAttackGainKeyword(
        #     "AttachedCharacter",
        # ),
        # AbilityFactory.PlayerActionToDiscardThis(
        #     AbilityType.,
        # ).SetCost(Cost())
        # .SetCostFunc(CostFunc.Exhaust("YourHero")),"""


        if card_type == "Obligation":
            obligation_func = f"""    def {action_name}(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
    def {action_name}(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )

"""
            obligation_ability = f"""
        AbilityFactory.WhenThisObligationGiveToYou(
            {action_name}
        ),
        AbilityFactory.WhenThisObligationRevealed(
            {action_name}
        ),
        # AbilityFactory.PlayerActionToDiscardThis(
        #     AbilityType.,
        # ).SetCost(Cost())
        # .SetCostFunc(CostFunc.Exhaust("YourIdentity")),"""

        if card_type in ["Treachery", "Villain", "MainScheme", "EncounterSideScheme", "Minion", "Environment", "Leader"]:
            reveal_func = f"""    def {action_name}_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

"""

            reveal_ability = f"""
        AbilityFactory.WhenThisRevealed(
            None,
            {action_name}_revealed
        ),"""

        if card_type in ['Minion']:

            defeated_func = f"""    def {action_name}_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

        player = message.GetDefeatingPlayer()

"""
            defeated_ability = f"""
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            {action_name}_defeated,
        ),"""

        if card_type in ['Upgrade']:
            asset_func = f""

            asset_ability = f"""
        # *AbilityFactory.GiveKeywordToAttached(
        #     Hero,
        #     ex_change_on_event=
        # ),"""

        if card_type in ['Attachment']:
            asset_func = f""

            asset_ability = f"""
        # *AbilityFactory.GiveKeywordToAttached(
        #     "Character",
        #     ex_change_on_event=
        # ),"""

        if card_type in [ "Hero", "AlterEgo", "Ally", "Upgrade", "Support", "Event"]:
            is_play = ""
            if card_type == "Event":
                is_play = ".SetPlay().SetLabel()"
            turn_func = f"""    def {action_name}(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo({card_type})
        Unused(this)

        initiator = effect.GetInitiator()

"""
            turn_ability = f"""
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.,
            {action_name}
        ){is_play},"""


        if card_type == "Evidence":

            evidence_func = f"""    def {action_name}(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(Evidence)
        Unused(this)

        def action(player: 'Player'):
            player.

        Players.ForEachPlayer(effect, action)
"""
            evidence_ability = f"""
        AbilityFactory.WhenCardSetup(
            "This",
            {action_name}
        ),"""

        ################################################################################
        #
        script_template = """from . import *

# {card_name}{subtitle}

def GetAbilities() -> Sequence['Ability']:

{asset_func}{turn_func}{reveal_func}{boost_func}{obligation_func}{env_func}{defeated_func}{evidence_func}
    return [{attach_ability}{asset_ability}{turn_ability}{reveal_ability}{boost_ability}{obligation_ability}{env_ability}{defeated_ability}{evidence_ability}
    ]

"""

        # ability_return_text_list = {
        #     "WhenYourPlayTurn": """
        #     AbilityFactory.WhenYourPlayTurn(
        #         AbilityType.,
        #         None,
        #         {action_name}
        #     )"""
        # }


        if 'abilities' in data and obj.ability_link == "":
            # abilities_return_text = ""
            # for ability in data['abilities']:
            #     if ability in ability_return_text_list:
            #         abilities_return_text += ability_return_text_list[ability]
            #     elif ability != 'Ability':
            #         abilities_return_text += f'\n        AbilityFactory.{ability}(),'
            # Log.Print(abilities_return_text)

            file_path = f'./cards/pack/{obj.pack}/{FileManager.CleanName(obj.set_name)}/{obj.card_id}.py'
            if FileManager.Exists(file_path):
                pass
            else:
                FileManager.MakeDir(FileManager.GetDirName(file_path))
                with open(file_path, "w") as file:
                    file.write(script_template.format(
                        action_name='{action_name}',
                        subtitle='{subtitle}',
                        turn_func=turn_func,
                        asset_func=asset_func,
                        asset_ability=asset_ability,
                        turn_ability=turn_ability,
                        reveal_ability=reveal_ability,
                        reveal_func=reveal_func,
                        obligation_func=obligation_func,
                        card_id=obj.card_id,
                        card_name=card_name,
                        card_type=card_type,
                        boost_func=boost_func,
                        boost_ability=boost_ability,
                        attach_ability=attach_ability,
                        obligation_ability=obligation_ability,
                        env_func=env_func,
                        env_ability=env_ability,
                        defeated_func=defeated_func,
                        defeated_ability=defeated_ability,
                        evidence_func=evidence_func,
                        evidence_ability=evidence_ability,
                        # abilities_return_text=abilities_return_text
                        ).format(action_name=action_name,subtitle=subtitle)
                    )

            FileManager.EditCode(file_path)

    AddEffect()
    Save(obj, card_name, desc)

