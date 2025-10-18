#!/bin/bash
# Script to verify GitHub Copilot whitelist URLs are accessible
# This script can be run to validate that whitelisted URLs are working

echo "===== GitHub Copilot URL Whitelist Verification ====="
echo ""

# Read URLs from the whitelist configuration
WHITELIST_FILE=".github/copilot-whitelist.json"

if [ ! -f "$WHITELIST_FILE" ]; then
    echo "ERROR: Whitelist file not found at $WHITELIST_FILE"
    exit 1
fi

echo "Reading URLs from: $WHITELIST_FILE"
echo ""

# Extract URLs using jq or grep
if command -v jq &> /dev/null; then
    URLS=$(jq -r '.allowed_urls[]' "$WHITELIST_FILE")
else
    echo "WARNING: jq not found, using grep fallback"
    URLS=$(grep -oP '"https?://[^"]+' "$WHITELIST_FILE" | tr -d '"')
fi

# Test each URL
SUCCESS_COUNT=0
FAIL_COUNT=0

for URL in $URLS; do
    echo "Testing: $URL"
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -L "$URL" 2>&1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        echo "  ✓ Status: $HTTP_CODE - OK"
        ((SUCCESS_COUNT++))
    else
        echo "  ✗ Status: $HTTP_CODE - FAILED"
        ((FAIL_COUNT++))
    fi
    echo ""
done

echo "===== Summary ====="
echo "Total URLs tested: $((SUCCESS_COUNT + FAIL_COUNT))"
echo "Successful: $SUCCESS_COUNT"
echo "Failed: $FAIL_COUNT"
echo ""

if [ $FAIL_COUNT -gt 0 ]; then
    echo "❌ Some URLs failed verification"
    exit 1
else
    echo "✅ All URLs verified successfully"
    exit 0
fi
