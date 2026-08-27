# Analyzer suppression registry

Every rule tuned down from the `IncludeAll=Error` baseline in `Assets/Default.ruleset`
gets one line here, format:

`RULE_ID — rationale (one line, why this rule doesn't serve this project)`

A test (`AnalyzerConfigTests.EveryTunedRule_HasARationale`) fails if a `<Rule>`
entry exists in Default.ruleset without a matching rationale line here.
Audits treat unexplained suppressions as findings.

<!-- No suppressions yet. -->
