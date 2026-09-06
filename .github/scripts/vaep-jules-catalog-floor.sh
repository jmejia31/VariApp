#!/usr/bin/env bash
set -euo pipefail

readonly BRANCH="Desarrollo"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly TARGET=24
readonly FLOOR=20

: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY required}"
: "${GH_TOKEN:?GH_TOKEN required}"
: "${WORKER_ID:?WORKER_ID required}"
command -v gh >/dev/null 2>&1 || exit 2
command -v jq >/dev/null 2>&1 || exit 2

case "$WORKER_ID" in
  JULES_A) DISPATCH_PATH="vaep/jules/dispatch"; LANE=A; TEST_ROOT="backend/tests/InventoryApp.Tests" ;;
  JULES_B) DISPATCH_PATH="vaep/jules-b/dispatch"; LANE=B; TEST_ROOT="backend/tests/InventoryApp.Tests" ;;
  JULES_C) DISPATCH_PATH="vaep/jules-c/dispatch"; LANE=C; TEST_ROOT="frontend/e2e" ;;
  JULES_D) DISPATCH_PATH="vaep/jules-d/dispatch"; LANE=D; TEST_ROOT="backend/tests/InventoryApp.Tests" ;;
  *) echo "CATALOG_FLOOR_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;
esac

api(){ gh api "$@"; }
head_sha(){ api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha'; }

facets_A=(ROUNDING_CONSERVATION CLOSED_PERIOD_READ_STABILITY ACCOUNT_GROUP_TOTALS DEBIT_CREDIT_NETTING DATE_BUCKET_TOTALS WAREHOUSE_GROUP_TOTALS MOVEMENT_TYPE_TOTALS PERIOD_EDGE_INCLUSION DECIMAL_SCALE_STABILITY EMPTY_SOURCE_TOTALS MIXED_SIGN_AGGREGATE REPEATED_QUERY_STABILITY SOURCE_ROW_CONSERVATION ACCOUNTING_PERIOD_SORT NULL_OPTIONAL_AMOUNT BOUNDARY_AMOUNT MULTI_PERIOD_TOTALS DUPLICATE_SOURCE_BEHAVIOR PROJECTION_TOTAL_PARITY ACCOUNT_STATUS_TOTALS ZERO_NEGATIVE_BALANCE YEAR_BOUNDARY_TOTALS MONTH_BOUNDARY_TOTALS AGGREGATE_DETERMINISM)
facets_B=(QUERY_COUNT_PARITY PAGINATION_LAST_PAGE DATE_RANGE_INVERSION QUERY_WHITESPACE_FILTER QUERY_CASE_FILTER MYSQL_DATE_BOUNDARY QUERY_TIE_ORDER CANCELLATION_PROPAGATION READ_ONLY_QUERY MULTI_FILTER_COMPOSITION NO_MATCH_PAGINATION OUT_OF_RANGE_PAGE PAGE_SIZE_BOUNDARY QUERY_PROJECTION_NULL MYSQL_DECIMAL_PARITY TRANSACTION_READ_CONSISTENCY QUERY_DUPLICATE_ROW QUERY_PERIOD_STATUS QUERY_EMPTY_FILTER QUERY_COMBINED_SORT MYSQL_NULL_MAPPING QUERY_RANGE_EXACT_EDGE PAGINATION_STABILITY QUERY_ERROR_CONTRACT)
facets_C=(FILTER_RESET PAGINATION_KEYBOARD EMPTY_STATE_A11Y EXPORT_ANNOUNCEMENT FILTER_FOCUS_RETURN PAGE_CHANGE_STATE DATE_FILTER_KEYBOARD SORT_A11Y_STATE REFRESH_STATE NO_RESULTS_RECOVERY EXPORT_FILTER_PARITY PAGINATION_A11Y TABLE_HEADER_A11Y LOADING_STATE ERROR_RECOVERY RANGE_EDGE REPEATED_SORT FILTER_CLEAR_KEYBOARD RESPONSIVE_TABLE ZERO_TOTAL_DISPLAY NEGATIVE_AMOUNT_DISPLAY DECIMAL_DISPLAY BACK_FORWARD_STATE RELOAD_FILTER_STATE)
facets_D=(CSV_FORMULA_INJECTION EXPORT_FILENAME_SANITIZATION UNAUTHORIZED_QUERY UNAUTHORIZED_EXPORT SENSITIVE_ERROR_REDACTION AUDIT_CORRELATION MALFORMED_DATE_SECURITY OVERSIZED_FILTER_SECURITY FORBIDDEN_FIELD_DISCLOSURE PROBLEM_DETAILS_REDACTION AUTHZ_POLICY_MATRIX AUDIT_FAILURE_PATH EXPORT_CONTENT_TYPE QUERY_INJECTION_LITERAL CONTROL_CHAR_INPUT AUTHZ_PAGINATION AUTHZ_FILTER_VARIANT EXPORT_ERROR_REDACTION AUDIT_USER_CONTEXT CORRELATION_ID_RESPONSE MALFORMED_PAGINATION EXPORT_HEADER_INJECTION AUTHZ_EMPTY_RESULT ERROR_STATUS_CONSISTENCY)

eval 'facets=("${facets_'"$LANE"'[@]}")'

build_scope(){
  local facet="$1" n="$2" stem
  if [[ "$LANE" == C ]]; then
    stem="$(tr '[:upper:]_' '[:lower:]-' <<<"$facet")"
    printf '%s/n4-10-financial-report-%s-%s.spec.ts' "$TEST_ROOT" "$stem" "$n"
  else
    stem="$(tr -cd '[:alnum:]' <<<"$facet")"
    printf '%s/N410AFinancialReport%s%sTests.cs' "$TEST_ROOT" "$stem" "$n"
  fi
}

task_number(){
  sed -n 's/^N4\.10\.A\.\([0-9][0-9]*\)\..*$/\1/p' <<<"$1"
}

count_unused(){
  local json="$1" listing used=0 total dispatch task n
  listing="$(api "repos/$GITHUB_REPOSITORY/contents/$DISPATCH_PATH?ref=$BRANCH" 2>/dev/null || printf '[]')"
  total="$(jq --arg w "$WORKER_ID" '.lanes[$w] | length' <<<"$json")"
  while IFS=$'\t' read -r dispatch task; do
    n="$(task_number "$task")"
    if jq -e --arg f "$dispatch.json" '.[]? | select(.name==$f)' <<<"$listing" >/dev/null; then
      used=$((used+1))
    elif [[ -n "$n" ]] && jq -e --arg prefix "N4-10-A-$n-" '.[]? | select((.name // "") | startswith($prefix))' <<<"$listing" >/dev/null; then
      # Different dispatchId, same material task identity (for example PREARM/recovery).
      # Count it as consumed so the hard-floor metric cannot hide duplicate ownership.
      used=$((used+1))
    fi
  done < <(jq -r --arg w "$WORKER_ID" '.lanes[$w][]? | [.dispatchId,.taskId] | @tsv' <<<"$json")
  printf '%s' "$((total-used))"
}

regenerate_lane(){
  local json="$1" max n facet scope task dispatch prompt entries='[]' i=0
  max="$(jq '[.lanes[][]?.taskId | try (capture("N4\\.10\\.A\\.(?<n>[0-9]+)").n | tonumber) catch empty] | max // 134' <<<"$json")"
  for facet in "${facets[@]}"; do
    i=$((i+1)); n=$((max+i)); scope="$(build_scope "$facet" "$n")"
    task="N4.10.A.${n}.${facet}_TESTS"
    dispatch="N4-10-A-${n}-${facet}-TESTS-${LANE}-AUTO"
    prompt="Read docs/VAEP_AUTHORITY.md first. AUTOREFILL material NEXT_SAFE for N4.10.A. Own ONLY ${scope}. Add a focused regression for the current behavior identified by ${facet}; reuse existing contracts only and do not invent APIs or production behavior. Run proportional tests/build and TWO independent self-reviews. No production/workflow/docs/manifest/dependency changes. No push/PR/branch/merge/deploy."
    entries="$(jq --arg d "$dispatch" --arg t "$task" --arg w "$WORKER_ID" --arg s "$scope only" --arg p "$prompt" '. + [{dispatchId:$d,taskId:$t,workerId:$w,taskAttempt:1,fileScopeHint:$s,prompt:$p}]' <<<"$entries")"
  done
  jq --arg w "$WORKER_ID" --argjson e "$entries" --arg now "$(date -u +%Y-%m-%dT%H:%M:%SZ)" '.lanes[$w]=$e | .generatedAt=$now | .currentParent="N4.10.A" | .regenerationReason="BACKLOG_HARD_FLOOR_REPLENISH"' <<<"$json"
}

commit_catalog(){
  local content="$1" head base tree blob newtree commit rc unused
  for _ in $(seq 1 8); do
    head="$(head_sha)"
    content="$(api "repos/$GITHUB_REPOSITORY/contents/$CATALOG?ref=$BRANCH" --jq '.content' | tr -d '\n' | base64 -d)"
    unused="$(count_unused "$content")"
    # The wrapper consumes one catalog entry immediately after this guard.
    # Require > FLOOR before handoff so post-dispatch UNUSED never falls below FLOOR.
    if (( unused > FLOOR )); then
      echo "CATALOG_FLOOR_OK worker=$WORKER_ID unused=$unused post_dispatch_min=$((unused-1))"
      return 0
    fi
    content="$(regenerate_lane "$content")"
    blob="$(jq -n --arg content "$content" '{content:$content,encoding:"utf-8"}' | api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - --jq '.sha')"
    base="$(api "repos/$GITHUB_REPOSITORY/git/commits/$head")"
    tree="$(jq -r '.tree.sha' <<<"$base")"
    newtree="$(jq -n --arg base "$tree" --arg path "$CATALOG" --arg sha "$blob" '{base_tree:$base,tree:[{path:$path,mode:"100644",type:"blob",sha:$sha}]}' | api "repos/$GITHUB_REPOSITORY/git/trees" --method POST --input - --jq '.sha')"
    commit="$(jq -n --arg tree "$newtree" --arg parent "$head" --arg msg "chore(vaep): replenish $WORKER_ID autorefill catalog floor" '{message:$msg,tree:$tree,parents:[$parent]}' | api "repos/$GITHUB_REPOSITORY/git/commits" --method POST --input - --jq '.sha')"
    set +e
    jq -n --arg sha "$commit" '{sha:$sha,force:false}' | api "repos/$GITHUB_REPOSITORY/git/refs/heads/$BRANCH" --method PATCH --input - >/dev/null 2>&1
    rc=$?
    set -e
    if (( rc == 0 )); then
      echo "CATALOG_FLOOR_REPLENISHED worker=$WORKER_ID target=$TARGET commit=$commit"
      return 0
    fi
    sleep 1
  done
  echo "CATALOG_FLOOR_ERROR=head_race_exhausted worker=$WORKER_ID" >&2
  return 3
}

current="$(api "repos/$GITHUB_REPOSITORY/contents/$CATALOG?ref=$BRANCH" --jq '.content' | tr -d '\n' | base64 -d)"
unused="$(count_unused "$current")"
# Because autorefill-core consumes one entry immediately after this script,
# replenish at the boundary too: UNUSED=20 must not be allowed to become 19.
if (( unused <= FLOOR )); then
  commit_catalog "$current"
else
  echo "CATALOG_FLOOR_OK worker=$WORKER_ID unused=$unused post_dispatch_min=$((unused-1))"
fi