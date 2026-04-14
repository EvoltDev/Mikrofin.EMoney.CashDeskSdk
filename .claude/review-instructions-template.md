# Review instructions
# Drop this file at .claude/review-instructions.md in any repo.
# The shared pr-review.yml workflow picks it up automatically.
# Delete any sections that don't apply to this project.

## Language and runtime
Language:
Framework:
Runtime version:

## Project-specific focus areas
<!-- List anything Claude should pay extra attention to in this codebase. -->
<!-- Examples: auth flows, database access patterns, API contracts, etc.   -->

## Conventions we enforce
<!-- Things that are not generic best-practice but specific to this team.  -->
<!-- Examples: naming rules, logging patterns, mandatory fields, etc.      -->

## Files and paths to skip
<!-- In addition to the default ignore list (generated files, lockfiles,   -->
<!-- migrations), skip these paths or patterns in this repo:               -->
# - src/generated/
# - legacy/

## Known issues to ignore
<!-- Patterns that trigger false positives in this codebase.               -->
<!-- Be specific — vague suppressions hide real bugs.                      -->
# - The XYZ pattern in src/compat/ is intentional for backwards compat.
