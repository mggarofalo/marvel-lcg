function Assert-ExpertCampaignUpgrade {
    param([string]$Published, [string]$CurrentDirectory, [string]$Choice,
        [string]$ReplayContract = 'engine-replay-v28')
    $id = '5721469c84d13a3550fd6d2f3950f301'
    $original = Read-CampaignGeneration "$Published/campaigns/$id"
    $current = Read-CampaignGeneration $CurrentDirectory
    $save = $current['save']; $before = $original['save']
    $compatibility = $save['compatibility'].DeepClone()
    if ($compatibility['replay_contract'].ToString() -cne $ReplayContract) { throw "Expert campaign was not upgraded to $ReplayContract." }
    if ($save['attempts'].Count -ne 7 -or $save['journal'].Count -ne $before['journal'].Count) { throw 'Expert history length changed.' }
    $save['compatibility'] = $before['compatibility'].DeepClone()
    for ($i = 0; $i -lt 7; $i++) {
        if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($compatibility, $save['attempts'][$i]['compatibility'])) { throw 'Attempt compatibility differs.' }
        $save['attempts'][$i]['compatibility'] = $before['attempts'][$i]['compatibility'].DeepClone()
    }
    for ($i = 0; $i -lt $save['journal'].Count; $i++) {
        if ($before['journal'][$i]['proof']) {
            if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($compatibility, $save['journal'][$i]['proof']['compatibility'])) { throw 'Proof compatibility differs.' }
            $save['journal'][$i]['proof']['compatibility'] = $before['journal'][$i]['proof']['compatibility'].DeepClone()
        }
    }
    if ($Choice) {
        $units = $save['attempts'][6]['units']
        if ($units.Count -ne 1 -or $units[0]['decisions'].Count -ne 1) { throw 'Expected exactly one recovery decision.' }
        $decision = $units[0]['decisions'][0]
        $expected = $Choice -eq 'decline'
        if ($decision['decision']['selector']['decline'].GetValue[bool]() -ne $expected) { throw 'Wrong recovery choice persisted.' }
        $attempt = $save['attempts'][6]
        if ($attempt['revision'].GetValue[int]() -ne 1 -or $attempt['cursor'].GetValue[int]() -ne 1 -or
            $attempt['current_prompt']['label'].ToString() -cne 'Spider-Man resolves mulligans' -or
            $save['revision'].GetValue[int]() -ne $before['revision'].GetValue[int]() + 1) { throw 'Recovery did not publish exactly its next mulligan prompt.' }
        # One accepted setup answer advances only this attempt's cursor, revision and prompt.
        foreach ($field in @('units', 'revision', 'cursor', 'current_prompt')) {
            $attempt[$field] = $before['attempts'][6][$field].DeepClone()
        }
        $save['revision'] = $before['revision'].DeepClone()
    }
    if (-not [Text.Json.Nodes.JsonNode]::DeepEquals($original, $current)) { throw 'Expert upgrade altered original history, proof, authority or record.' }
    $backup = @(Get-ChildItem $CurrentDirectory -Force -Filter '.upgrade-*.archive')
    $manifest = Get-Content "$Published/campaigns/$id/current" -Raw | ConvertFrom-Json
    if ($backup.Count -ne 1 -or (Get-FileHash $backup[0].FullName).Hash -cne $manifest.sha256) { throw 'Original Expert migration backup bytes changed.' }
}
