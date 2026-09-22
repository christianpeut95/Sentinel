# Background geocoding

When enabled, Sentinel can geocode addresses from newly created HL7 patients in
the background. This prevents an external geocoding call from delaying HL7
message processing.

## Behaviour

1. HL7 intake creates the patient and enqueues a non-empty address.
2. The hosted geocoding worker dequeues ready items, calls the configured
   `IGeocodingService`, and saves returned latitude and longitude on the
   patient.
3. Failed items are retried after approximately one, five and fifteen minutes.
   After the third failed attempt they are recorded in the worker's daily
   failure statistics.

The active queue and daily statistics are in memory. A process restart discards
pending work, so this mechanism is best-effort rather than a durable job queue.
The worker is not a substitute for validating an address at data-entry time.

## Configuration

```json
"Geocoding": {
  "Provider": "Nominatim",
  "BackgroundProcessing": true,
  "MaxConcurrentRequests": 2,
  "DelayBetweenRequestsMs": 250,
  "CheckIntervalMs": 5000,
  "TimeoutMs": 10000
}
```

The provider configuration and any server-side API key are installation
settings. Do not store secrets in source control. Choose limits appropriate for
the provider's documented quota and the organisation's privacy requirements.

## Current limitation

The worker begins jurisdiction-detection work after successful geocoding, but
automatic polygon-based jurisdiction assignment is not implemented. Do not
rely on background geocoding alone to assign a patient to a jurisdiction.
