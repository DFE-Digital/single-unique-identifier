#!/bin/bash
# Retries a command up to 3 times with exponential backoff.

retry_command() {
  local max_attempts=3
  local attempt=1
  local delay=5

  while [ $attempt -le $max_attempts ]; do
    echo "Executing (Attempt $attempt): $@"
    if "$@"; then
      return 0
    fi
    echo "Command failed. Retrying in $delay seconds..."
    sleep $delay
    attempt=$((attempt + 1))
    delay=$((delay * 2))
  done
  return 1
}