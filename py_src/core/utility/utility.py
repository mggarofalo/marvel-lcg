
def Unquote(string: str):
    from urllib.parse import unquote
    return unquote(string, 'utf-8', 'replace')
