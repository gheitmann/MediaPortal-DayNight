# Validation — v0.2.0-alpha.1



The source was extracted byte-for-byte from the locally verified Backdrop

0.2.0.0 prototype. Standalone build and probe results are recorded below before

publication. Static CI checks language completeness and the integration example.

It does not download or redistribute MediaPortal runtime binaries.



The Backdrop integration passed 1,364/1,364 assertions on each of x86 and x64,

covering native expressions, settings, migration, languages and lifecycle.

The full x64 client applied Morning and Noon, saved preferences, discarded a

staged minute change and returned to Automatic without restarting.



Full x86 client interaction, prolonged playback, suspend/resume and visual

inspection of every language remain unverified. No broad cross-version claim

is made. Existing log4net configuration warnings appear in native probe hosts.


Standalone native probes passed 2,201/2,201 assertions on each architecture,
including every minute of a complete default day, six modes, persistence,
migration, invalid settings, skin fallback and all 37 language catalogues.
The static source check passed. The release ZIP is reopened and every byte
compared; no MediaPortal binaries or artwork are included.
