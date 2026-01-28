#!/usr/bin/env bash
# This script is a small guardrail to ensure the CI security job is not
# accidentally removed or broken. It verifies that the security-and-deps
# job and its critical steps still exist in .github/workflows/pr-ci.yml,
# and fails the pipeline if they do not. If everything is present, it prints:
#   CI security and dependency checks appear to be configured correctly.
set -euo pipefail

WORKFLOW_FILE=".github/workflows/pr-ci.yml"
JOB_NAME="security-and-deps"

if [[ ! -f "$WORKFLOW_FILE" ]]; then
  echo "Expected workflow file '$WORKFLOW_FILE' not found." >&2
  exit 1
fi

if ! grep -q "^  ${JOB_NAME}:" "$WORKFLOW_FILE"; then
  echo "Expected CI job '${JOB_NAME}' is missing from $WORKFLOW_FILE" >&2
  exit 1
fi

if ! grep -q "dotnet list package --vulnerable --include-transitive" "$WORKFLOW_FILE"; then
  echo "Expected vulnerable package check step is missing from $WORKFLOW_FILE" >&2
  exit 1
fi

if ! grep -q "Generate SBOM" "$WORKFLOW_FILE"; then
  echo "Expected SBOM generation step is missing from $WORKFLOW_FILE" >&2
  exit 1
fi

if ! grep -q "Upload SBOM artifact" "$WORKFLOW_FILE"; then
  echo "Expected SBOM upload step is missing from $WORKFLOW_FILE" >&2
  exit 1
fi

echo "CI security and dependency checks appear to be configured correctly."