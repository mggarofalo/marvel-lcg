export function sanitizeString(str: string) {
    return str.replace(/[^a-z0-9]/gi, '_').toLowerCase();
}

export function saveAsFile(text: string, file_name: string) {
    var dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(text);
    let name = sanitizeString(file_name)
    var downloadAnchorNode = document.createElement('a');
    downloadAnchorNode.setAttribute("href", dataStr);
    downloadAnchorNode.setAttribute("download", `${file_name}`);
    document.body.appendChild(downloadAnchorNode); // required for firefox
    downloadAnchorNode.click();
    downloadAnchorNode.remove();
}

