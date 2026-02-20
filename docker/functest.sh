#!/bin/bash
set -euo pipefail

API="http://localhost:5010/api/v1"

# Get token (URL-encode special chars: ! -> %21, @ -> %40)
TOKEN_RESP=$(curl -s "http://localhost:5001/connect/token" \
  -d "grant_type=password" \
  -d "client_id=hmm.functest" \
  -d "client_secret=FuncTestSecret123%21" \
  -d "username=testuser%40hmm.local" \
  -d "password=TestPassword123%21")
TOKEN=$(echo "$TOKEN_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")
echo "Token obtained successfully"
echo ""

PASS=0
FAIL=0

check() {
  local label="$1" actual="$2" expected="$3"
  if [ "$actual" = "$expected" ]; then
    echo "  [PASS] $label (got: $actual)"
    PASS=$((PASS+1))
  else
    echo "  [FAIL] $label (expected: $expected, got: $actual)"
    FAIL=$((FAIL+1))
  fi
}

jval() { echo "$1" | python3 -c "import sys,json; print(json.load(sys.stdin).get('$2',''))" 2>/dev/null; }
jcode() { echo "$1" | tail -1; }
jbody() { echo "$1" | sed '$d'; }

# Helper: request with status code
req() {
  local method="$1" url="$2" data="${3:-}"
  if [ -n "$data" ]; then
    curl -s -w "\n%{http_code}" -X "$method" "$url" \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "$data"
  else
    curl -s -w "\n%{http_code}" -X "$method" "$url" -H "Authorization: Bearer $TOKEN"
  fi
}

echo "=========================================="
echo "=== 1. AUTOMOBILE CRUD ==="
echo "=========================================="

# CREATE (VIN must be exactly 17 chars, Model is required)
RESP=$(req POST "$API/automobiles" '{"vin":"1HGBH41JXMN109186","maker":"Toyota","brand":"Corolla","model":"LE","year":2024,"color":"Blue","plate":"FT-001","meterReading":1000,"fuelType":"Regular","engineType":"Gasoline"}')
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
AUTO_ID=$(jval "$BODY" id)
check "CREATE status" "$CODE" "200"
echo "  Created ID: $AUTO_ID"

# READ
RESP=$(req GET "$API/automobiles/$AUTO_ID")
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
check "READ status" "$CODE" "200"
check "READ isActive" "$(jval "$BODY" isActive)" "True"
check "READ color" "$(jval "$BODY" color)" "Blue"

# UPDATE
RESP=$(req PUT "$API/automobiles/$AUTO_ID" '{"color":"Red","plate":"FT-002","meterReading":2000}')
CODE=$(jcode "$RESP")
check "UPDATE status" "$CODE" "204"

RESP=$(req GET "$API/automobiles/$AUTO_ID")
BODY=$(jbody "$RESP")
check "UPDATE verify color" "$(jval "$BODY" color)" "Red"
check "UPDATE verify plate" "$(jval "$BODY" plate)" "FT-002"

# DELETE (soft)
RESP=$(req DELETE "$API/automobiles/$AUTO_ID")
CODE=$(jcode "$RESP")
check "DELETE status" "$CODE" "204"

RESP=$(req GET "$API/automobiles/$AUTO_ID")
BODY=$(jbody "$RESP")
check "DELETE verify isActive=False" "$(jval "$BODY" isActive)" "False"

echo ""
echo "=========================================="
echo "=== 2. GAS STATION CRUD ==="
echo "=========================================="

# CREATE (no brand field - uses Name, Address, City, etc.)
RESP=$(req POST "$API/automobiles/gasstations" '{"name":"Shell FT","address":"123 Main St","city":"Vancouver","isActive":true}')
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
STATION_ID=$(jval "$BODY" id)
check "CREATE status" "$CODE" "200"
echo "  Created ID: $STATION_ID"

# READ by ID
RESP=$(req GET "$API/automobiles/gasstations/$STATION_ID")
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
check "READ status" "$CODE" "200"
check "READ name" "$(jval "$BODY" name)" "Shell FT"

# UPDATE
RESP=$(req PUT "$API/automobiles/gasstations/$STATION_ID" '{"name":"Shell Updated","address":"456 Oak Ave"}')
CODE=$(jcode "$RESP")
check "UPDATE status" "$CODE" "204"

RESP=$(req GET "$API/automobiles/gasstations/$STATION_ID")
BODY=$(jbody "$RESP")
check "UPDATE verify name" "$(jval "$BODY" name)" "Shell Updated"

# DELETE (soft) - returns 200 with no body, verify by re-reading
RESP=$(req DELETE "$API/automobiles/gasstations/$STATION_ID")
CODE=$(jcode "$RESP")
check "DELETE status" "$CODE" "200"

RESP=$(req GET "$API/automobiles/gasstations/$STATION_ID")
BODY=$(jbody "$RESP")
check "DELETE verify isActive=False" "$(jval "$BODY" isActive)" "False"

echo ""
echo "=========================================="
echo "=== 3. GAS DISCOUNT CRUD ==="
echo "=========================================="

# CREATE (requires: program, amount, discountType)
RESP=$(req POST "$API/automobiles/gaslogs/discounts" '{"program":"FT Rewards","amount":0.10,"currency":"CAD","discountType":"PerLiter","isActive":true}')
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
DISC_ID=$(jval "$BODY" id)
check "CREATE status" "$CODE" "200"
echo "  Created ID: $DISC_ID"

# READ
RESP=$(req GET "$API/automobiles/gaslogs/discounts/$DISC_ID")
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
check "READ status" "$CODE" "200"
check "READ program" "$(jval "$BODY" program)" "FT Rewards"

# UPDATE
RESP=$(req PUT "$API/automobiles/gaslogs/discounts/$DISC_ID" '{"program":"FT Rewards Plus","amount":0.15,"currency":"CAD","discountType":"PerLiter","isActive":true}')
CODE=$(jcode "$RESP")
check "UPDATE status" "$CODE" "204"

# DELETE (soft) - returns 200 with no body, verify by re-reading
RESP=$(req DELETE "$API/automobiles/gaslogs/discounts/$DISC_ID")
CODE=$(jcode "$RESP")
check "DELETE status" "$CODE" "200"

RESP=$(req GET "$API/automobiles/gaslogs/discounts/$DISC_ID")
BODY=$(jbody "$RESP")
check "DELETE verify isActive=False" "$(jval "$BODY" isActive)" "False"

echo ""
echo "=========================================="
echo "=== 4. GAS LOG CRUD ==="
echo "=========================================="

# Need an active automobile and station first
RESP=$(req POST "$API/automobiles" '{"vin":"2HGBH41JXMN109187","maker":"Honda","brand":"Civic","model":"EX","year":2023,"color":"White","plate":"GL-001","meterReading":5000,"fuelType":"Regular","engineType":"Gasoline"}')
BODY=$(jbody "$RESP")
GL_AUTO_ID=$(jval "$BODY" id)
echo "  Automobile for GasLog: ID=$GL_AUTO_ID"

RESP=$(req POST "$API/automobiles/gasstations" '{"name":"Costco Gas","address":"789 Elm","city":"Vancouver","isActive":true}')
BODY=$(jbody "$RESP")
GL_STATION_ID=$(jval "$BODY" id)
echo "  Station for GasLog: ID=$GL_STATION_ID"

# CREATE GasLog (uses flat fields, not nested objects)
RESP=$(req POST "$API/automobiles/$GL_AUTO_ID/gaslogs" "{\"date\":\"2024-06-15T10:00:00Z\",\"automobileId\":$GL_AUTO_ID,\"stationId\":$GL_STATION_ID,\"distance\":350,\"distanceUnit\":\"Kilometre\",\"odometer\":5350,\"odometerUnit\":\"Kilometre\",\"fuel\":40,\"fuelUnit\":\"Liter\",\"fuelGrade\":\"Regular\",\"unitPrice\":1.55,\"totalPrice\":62.00,\"currency\":\"CAD\",\"isFullTank\":true}")
CODE=$(jcode "$RESP"); BODY=$(jbody "$RESP")
# GasLog response may use PascalCase keys (Id vs id) or be wrapped in {value:{...}}
GASLOG_ID=$(echo "$BODY" | python3 -c "
import sys,json
d=json.load(sys.stdin)
# Try camelCase, PascalCase, or nested value object
print(d.get('id', d.get('Id', d.get('value',{}).get('Id', d.get('value',{}).get('id','')))))" 2>/dev/null)
check "CREATE status" "$CODE" "200"
echo "  Created GasLog ID: $GASLOG_ID"

if [ -n "$GASLOG_ID" ] && [ "$GASLOG_ID" != "" ]; then
  # READ
  RESP=$(req GET "$API/automobiles/$GL_AUTO_ID/gaslogs/$GASLOG_ID")
  CODE=$(jcode "$RESP")
  check "READ status" "$CODE" "200"

  # LIST
  RESP=$(req GET "$API/automobiles/$GL_AUTO_ID/gaslogs")
  CODE=$(jcode "$RESP")
  check "LIST status" "$CODE" "200"
else
  echo "  [SKIP] GasLog CREATE failed, skipping remaining tests"
  echo "  Response body: $BODY"
  FAIL=$((FAIL+2))
fi

echo ""
echo "=========================================="
echo "=== SUMMARY ==="
echo "=========================================="
echo "Passed: $PASS"
echo "Failed: $FAIL"
echo "Total:  $((PASS+FAIL))"
if [ "$FAIL" -eq 0 ]; then
  echo "ALL TESTS PASSED"
else
  echo "SOME TESTS FAILED"
fi
