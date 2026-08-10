# Editor Guide

## Prepare

1. Edit [`py_src/build.py`](../py_src/build.py), replace `release = True` with `release = False`
1. Start the game as normal — see the [Install Guide](install_guide.md)
1. Goto http://localhost:2340 to open the editor

## In the editor

### Load JSON Data

1. Fill in the `ID` field
1. Click the `Load Json` button
1. The editor will download the card JSON from https://marvelcdb.com/ and populate  the editor fields.

Notice: any fields with a green background will NOT be auto-filled, you must check and fill them yourself.

### Create a card

1. Check the "Ability" checkbox to mark this card as having abilities.
1. Click the "Submit" button to add this card.
1. If this card includes abilities, the editor will also create `{card_id}.py` file and open it in vscode for editing.

![](/docs/assets/image-3.jpg)
