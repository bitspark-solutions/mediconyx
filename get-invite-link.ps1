$messages = Invoke-RestMethod -Uri "http://localhost:8025/api/v2/messages" -Method Get
$inviteEmail = $messages.items | Where-Object { $_.Content.Headers.Subject[0] -like "*invited to set up*" } | Select-Object -First 1
if ($inviteEmail) {
    $body = $inviteEmail.Content.Body
    # Decode quoted-printable if needed
    if ($inviteEmail.Content.Headers.'Content-Transfer-Encoding' -contains 'quoted-printable') {
        $body = $body -replace '=\r?\n', '' -replace '=3D', '=' -replace '=2F', '/'
    }
    # Extract the accept-invite link
    if ($body -match 'href=[''"]?(http://localhost:9673/accept-invite\?token=[^''" >]+)') {
        Write-Host "Invitation link: $($Matches[1])"
    } else {
        Write-Host "Link not found in body. Body preview:"
        Write-Host $body.Substring(0, [Math]::Min(500, $body.Length))
    }
} else {
    Write-Host "No invitation email found"
}
