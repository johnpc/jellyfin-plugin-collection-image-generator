#!/bin/bash
# Enforce maximum line count per source file.
# Usage: bash scripts/check-line-count.sh [max_lines]
# Default max: 150 lines

MAX_LINES=${1:-250}
EXIT_CODE=0

echo "File Line Count Check (max: ${MAX_LINES} lines)"
echo "============================================================"

while IFS= read -r file; do
    lines=$(wc -l < "$file")
    if [ "$lines" -gt "$MAX_LINES" ]; then
        echo "FAIL: ${file} (${lines} lines)"
        EXIT_CODE=1
    fi
done < <(find . -name "*.cs" \
    -not -path "*/bin/*" \
    -not -path "*/obj/*" \
    -not -path "*Tests*" \
    -not -path "*AcceptanceTests*" \
    | sort)

if [ $EXIT_CODE -eq 0 ]; then
    echo "All source files are within the ${MAX_LINES}-line limit."
fi

exit $EXIT_CODE
