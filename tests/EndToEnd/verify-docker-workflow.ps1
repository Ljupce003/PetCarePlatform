$ErrorActionPreference = 'Stop'

$ownerId = '33333333-3333-3333-3333-333333333333'
$petId = '44444444-4444-4444-4444-444444444444'
$gateway = 'http://localhost:7000'

$token = (Invoke-RestMethod -Method Post `
    -Uri 'http://localhost:8080/realms/petcare/protocol/openid-connect/token' `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{
        grant_type = 'password'
        client_id = 'petcare-demo'
        username = 'owner1'
        password = 'Owner123!'
    }).access_token
$headers = @{ Authorization = "Bearer $token" }

$pet = Invoke-RestMethod -Uri "$gateway/pet/pets/$petId" -Headers $headers
$slots = Invoke-RestMethod -Uri "$gateway/appointment/slots" -Headers $headers
if ($slots.Count -eq 0) { throw 'No open appointment slots are available.' }
$slot = $slots[0]

$appointment = Invoke-RestMethod -Method Post `
    -Uri "$gateway/appointment/appointments" `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{
        petId = $petId
        ownerId = $ownerId
        availabilitySlotId = [string]$slot.availabilitySlotId
        reason = 'Docker end-to-end verification'
    } | ConvertTo-Json)

$notification = $null
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    Start-Sleep -Milliseconds 500
    $notifications = Invoke-RestMethod `
        -Uri "$gateway/treatment/api/notifications/owner/$ownerId" `
        -Headers $headers
    $notification = $notifications |
        Where-Object { $_.petId -eq $petId -and $_.title -eq 'Appointment scheduled' } |
        Select-Object -First 1
    if ($null -ne $notification) { break }
}

if ($null -eq $notification) {
    throw 'Kafka notification was not visible after 15 seconds.'
}

Write-Output "Login: owner1"
Write-Output "Pet: $($pet.name) ($($pet.petId))"
Write-Output "Appointment: $($appointment.appointmentId)"
Write-Output "Kafka notification: $($notification.id)"
