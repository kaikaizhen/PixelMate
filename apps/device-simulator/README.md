# device-simulator

A tiny Node (≥ 18) command line that behaves like an ESP32 talking to the PixelMate API: no cookie, no CSRF header, one Bearer credential. It checks the
**API contract and data sync** (pair → snapshot → events → mail). It does not test the screen, touch, Wi-Fi or any real hardware.

```
node sim.mjs pair [--api http://localhost:5200] [--model Waveshare-AMOLED-2.16]   # shows a code, waits for the owner, stores the credential in .device.json
node sim.mjs snapshot      # GET /device/snapshot  (pet, equipment, activity, world/weather, unread mail, latest event number)
node sim.mjs events --after 0
node sim.mjs mail          # the owner's delivered letters
node sim.mjs open <letterId>
node sim.mjs watch         # poll like the firmware would, print new events
node sim.mjs rotate        # replace the credential
node sim.mjs unpair
```

## The device API (what the firmware implements)

| Step | Call | Notes |
|---|---|---|
| 1 | `POST /api/v1/device/pairing/start {model}` | returns `code` (show it on screen), `pollToken` (keep in RAM), `pairingId`, `expiresAt` (10 min), `pollIntervalSeconds` |
| 2 | the owner types the code in the app: `POST /api/v1/devices/pair {code, name?}` | cookie session, needs a pet; max 3 devices per pet |
| 3 | `POST /api/v1/device/pairing/poll {pairingId, pollToken}` every `pollIntervalSeconds` | `pending` → `approved` (`credential`, `deviceId`, `petId`) → afterwards `delivered`. The credential is returned **once**; store it in protected NVS |
| 4 | `Authorization: Bearer <credential>` on `GET /device/snapshot`, `GET /device/events?after=<seq>`, `GET /device/mail`, `GET /device/mail/{id}`, `POST /device/mail/{id}/read` | poll every few seconds at first; `eventId`/`seq` de-duplicate |
| – | `POST /device/credential/rotate`, `DELETE /device/me` | rotate / unpair; the owner can also revoke from the app (`DELETE /api/v1/devices/{id}`) |

Errors are `{ "code", "message", "requestId" }`; `401` means the credential is gone (revoked or rotated elsewhere): start pairing again. `429` on pairing/poll: slow down.
