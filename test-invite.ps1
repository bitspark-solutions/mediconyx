$loginBody = @{ email = "sales@mediconyx.local"; password = "Sales@123" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Uri "http://localhost:9765/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
$token = $loginResponse.accessToken
Write-Host "Login successful. Token: $($token.Substring(0, 20))..."

# Create a sales request
$requestBody = @{
    hospitalName = "Test Invitation Hospital"
    contactPerson = "Dr. Jane Smith"
    email = "jane.smith@testhospital.com"
    phone = "01700000000"
    region = "Dhaka"
    notes = "Testing invitation flow"
    captchaToken = "test-token"
} | ConvertTo-Json

$headers = @{ Authorization = "Bearer $token" }
$createResponse = Invoke-RestMethod -Uri "http://localhost:9765/api/sales/requests/public" -Method Post -ContentType "application/json" -Body $requestBody
Write-Host "Created request ID: $($createResponse.id)"

# Qualify the request (triggers auto-invite)
$qualifyResponse = Invoke-RestMethod -Uri "http://localhost:9765/api/sales/requests/$($createResponse.id)/qualify" -Method Patch -Headers $headers
Write-Host "Request qualified. Status: $($qualifyResponse.status)"

# Get invitations for this request
$invitations = Invoke-RestMethod -Uri "http://localhost:9765/api/invitations/by-request/$($createResponse.id)" -Method Get -Headers $headers
Write-Host "Invitations found: $($invitations.Count)"
if ($invitations.Count -gt 0) {
    Write-Host "Invitation status: $($invitations[0].status)"
    Write-Host "Invitation email: $($invitations[0].email)"
}
