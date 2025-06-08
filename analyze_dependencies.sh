#!/bin/bash

echo "=== PMB DEPENDENCY ANALYSIS ==="
echo "Analyzing dependencies across entire codebase..."
echo ""

echo "1. PHASE 3 DEPENDENCIES (Claims Processing):"
echo "Files that reference ServiceRecord, ClaimsIn, UnitRecord:"
find ./src -name "*.cs" -exec grep -l "ServiceRecord\|ClaimsIn\|UnitRecord" {} \; | sort
echo ""

echo "2. PHASE 4 DEPENDENCIES (Authentication):"
echo "Files that reference Account, Auth, User, Session:"
find ./src -name "*.cs" -o -name "*.js" | xargs grep -l "Account\|Auth\|User\|Session\|Login" | sort
echo ""

echo "3. PHASE 5 DEPENDENCIES (Frontend):"
echo "JavaScript and View files:"
find ./src -name "*.js" -o -name "*.cshtml" | wc -l
echo ""

echo "4. PHASE 6 DEPENDENCIES (Background Services):"
echo "Console applications:"
find ./src -type d -name "*MBS*" | grep -E "(Retrieve|Reconcile|Submit|Test|Password)" | sort
echo ""

echo "5. CROSS-PROJECT REFERENCES:"
find ./src -name "*.csproj" -exec grep -H "ProjectReference" {} \;
