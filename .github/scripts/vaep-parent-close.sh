#!/usr/bin/env bash
set -euo pipefail

readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly PARSER=".github/scripts/vaep-policy-parser.sh"
readonly CATALOG="vaep/control/jules-autorefill-catalog.json"
readonly ADMISSION="vaep/control/dispatch-admission.json"
readonly TRANSITION="scripts/vaep/parent_transition.py"
readonly BRANCH="Desarrollo"

require_runtime() {
  : "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY required}"
  : "${GH_TOKEN:?GH_TOKEN required}"
  [[ "$GITHUB_REPOSITORY" == "jmejia31/VariApp" ]] || { echo 'VAEP_CLOSE_ERROR=wrong_repository' >&2; exit 2; }
  for tool in gh jq python3 git; do
    command -v "$tool" >/dev/null 2>&1 || { echo "VAEP_CLOSE_ERROR=${tool}_missing" >&2; exit 2; }
  done
}

api() { gh api "$@"; }
current_head() { api "repos/$GITHUB_REPOSITORY/git/ref/heads/$BRANCH" --jq '.object.sha'; }

is_control_plane_path() {
  case "$1" in
    # This classifier changes the causal meaning of integration receipts and is
    # therefore a functional VAEP gate change, not evidence-only telemetry.
    # N5.2.C's reviewed receipt deliberately gates this exact functional head.
    scripts/vaep/jules_integration_metrics.py) return 1 ;;
    AGENTS.md|README.md|CHANGELOG_AI.md|PLAN_EJECUCION_AUTONOMA.md|PROJECT_CONTEXT.md|*.md|docs/*|docs/N4.11_CENTROS_COSTO_*.md|vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|vaep/j5/dispatch/*.json|vaep/j6/dispatch/*.json|vaep/control/*|vaep/evidence/*|scripts/vaep/*|.github/scripts/vaep-*|.github/workflows/vaep-*) return 0 ;;
    *) return 1 ;;
  esac
}

functional_head() {
  local head="$1" sha paths all_control path
  # The checkpoint has a full fetch. Inspect verified local Git objects instead
  # of exhausting the API after a long run of evidence-only commits.
  git cat-file -e "$head^{commit}" || return 1
  while IFS= read -r sha; do
    paths="$(git diff-tree --root --no-commit-id --name-only -r --first-parent -m "$sha")" || return 1
    all_control=true
    while IFS= read -r path; do
      [[ -n "$path" ]] || continue
      if ! is_control_plane_path "$path"; then all_control=false; break; fi
    done <<<"$paths"
    if [[ "$all_control" == false ]]; then printf '%s\n' "$sha"; return 0; fi
  done < <(git rev-list --first-parent --max-count=1000 "$head")
  echo 'VAEP_CLOSE_ERROR=functional_head_not_proven_within_bound' >&2
  return 1
}

latest_valid_fragment() {
  local parent="$1" file
  while IFS= read -r file; do
    [[ -f "$file" ]] || continue
    if jq -e --arg parent "$parent" '
      def parent_id: (.parent // .parentId // "");
      def legacy_contract:
        .decision == "LISTO_REAL" and
        .review == "PASS" and
        .combinedStatus == "SUCCESS" and
        .causalGates == "TERMINAL_SUCCESS_APPLICABLE" and
        .productionTouched == false and .mergePerformed == false;
      def current_contract:
        .state == "LISTO_REAL" and
        (.review | type == "object") and
        (.review.mode | type == "string" and length > 0) and
        .headRevalidated == true and
        (.causalGates | type == "array" and length > 0) and
        all(.causalGates[]; type == "object" and .conclusion == "success") and
        .mainTouched == false and .prMerged == false;
      type == "object" and .authority == "docs/VAEP_AUTHORITY.md" and
      parent_id == $parent and
      (.functionalHead | type == "string" and test("^[0-9a-fA-F]{40}$")) and
      (.p0Open | type == "number") and .p0Open == 0 and
      (.p1Open | type == "number") and .p1Open == 0 and
      (legacy_contract or current_contract)
    ' "$file" >/dev/null; then printf '%s\n' "$file"; return 0; fi
  done < <(find vaep/evidence/fragments -maxdepth 1 -type f -name "${parent}_LISTO_REAL_*.json" -print 2>/dev/null | sort -r)
  return 1
}

latest_push_success() {
  local head="$1"
  jq -e --arg head "$head" '
    [.workflow_runs[]? | select(.head_sha == $head and .head_branch == "Desarrollo" and .event == "push")]
    | sort_by(.created_at, .id) | last
    | .status == "completed" and .conclusion == "success"
  ' >/dev/null
}

declared_causal_gates_ok() {
  local functional="$1" fragment="$2"
  local workflow run_id expected_head expected_conclusion run actual_name count=0 functional_seen=0
  while IFS=$'\t' read -r workflow run_id expected_head expected_conclusion; do
    [[ -n "$workflow" ]] || { echo 'VAEP_CLOSE_CAUSAL_GATE_INVALID=WORKFLOW_MISSING' >&2; return 1; }
    [[ "$run_id" =~ ^[0-9]+$ ]] || { echo "VAEP_CLOSE_CAUSAL_GATE_INVALID=RUN_ID value=$run_id" >&2; return 1; }
    [[ "$expected_head" =~ ^[0-9a-fA-F]{40}$ ]] || { echo "VAEP_CLOSE_CAUSAL_GATE_INVALID=HEAD value=$expected_head" >&2; return 1; }
    [[ "$expected_conclusion" == "success" ]] || { echo "VAEP_CLOSE_CAUSAL_GATE_INVALID=RECEIPT_CONCLUSION run_id=$run_id value=$expected_conclusion" >&2; return 1; }
    if ! run="$(api "repos/$GITHUB_REPOSITORY/actions/runs/$run_id")"; then
      echo "VAEP_CLOSE_CAUSAL_GATE_INVALID=RUN_FETCH_FAILED run_id=$run_id" >&2
      return 1
    fi
    if ! jq -e --arg head "$expected_head" '
      .head_sha == $head and
      .status == "completed" and
      .conclusion == "success"
    ' <<<"$run" >/dev/null; then
      echo "VAEP_CLOSE_CAUSAL_GATE_INVALID=RUN_STATE run_id=$run_id expected_head=$expected_head" >&2
      return 1
    fi
    actual_name="$(jq -r '.name // ""' <<<"$run")"
    if [[ "$actual_name" != "$workflow" ]]; then
      echo "VAEP_CLOSE_CAUSAL_GATE_NAME_DIAGNOSTIC run_id=$run_id receipt_workflow=$workflow actual_workflow=$actual_name" >&2
    fi
    [[ "$expected_head" == "$functional" ]] && functional_seen=1
    count=$((count + 1))
  done < <(jq -r '.causalGates[] | [.workflow, (.runId | tostring), .headSha, .conclusion] | @tsv' "$fragment")
  if (( count == 0 || functional_seen == 0 )); then
    echo "VAEP_CLOSE_CAUSAL_GATE_INVALID=FUNCTIONAL_HEAD_NOT_COVERED functional_head=$functional gate_count=$count" >&2
    return 1
  fi
}

critical_gates_ok() {
  local functional="$1" fragment="$2" build acceptance recovery
  if jq -e '.causalGates | type == "array" and length > 0' "$fragment" >/dev/null; then
    declared_causal_gates_ok "$functional" "$fragment" || return 1
    echo "VAEP_CLOSE_CAUSAL_GATES=DECLARED_RECEIPT_VERIFIED functional_head=$functional"
  else
    # Backward-compatible fallback for legacy receipts that predate explicit
    # run IDs/head SHAs. New receipts must use the declared causal-gate path.
    build="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/desarrollo-ci.yml/runs?branch=$BRANCH&head_sha=$functional&event=push&per_page=10")" || return 1
    acceptance="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/catalogos-aceptacion.yml/runs?branch=$BRANCH&head_sha=$functional&event=push&per_page=10")" || return 1
    latest_push_success "$functional" <<<"$build" || return 1
    latest_push_success "$functional" <<<"$acceptance" || return 1
    echo "VAEP_CLOSE_CAUSAL_GATES=LEGACY_FALLBACK_VERIFIED functional_head=$functional"
  fi
  if migration_gate_applicable "$functional"; then
    recovery="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/migration-recovery-desarrollo.yml/runs?branch=$BRANCH&head_sha=$functional&event=push&per_page=10")" || return 1
    latest_push_success "$functional" <<<"$recovery" || return 1
    echo "VAEP_CLOSE_MIGRATION_GATE=REQUIRED functional_head=$functional"
  else
    echo "VAEP_CLOSE_MIGRATION_GATE=NOT_APPLICABLE functional_head=$functional"
  fi
}

migration_gate_applicable() {
  local functional="$1" commit
  commit="$(api "repos/$GITHUB_REPOSITORY/commits/$functional")" || return 0
  jq -e 'any(.files[]?.filename;
    startswith("backend/src/Infrastructure/Migrations/") or
    startswith("backend/src/Infrastructure/Persistence/Migrations/") or
    . == "backend/src/API/Program.cs" or
    . == ".github/workflows/migration-recovery-desarrollo.yml")' <<<"$commit" >/dev/null
}

live_jules_runs() {
  local workflow runs count total=0
  # Canonical runtime is J1-J6. A parent must never promote while any trusted
  # worker lane is still queued/running, regardless of legacy A-D history.
  for workflow in vaep-jules-j1.yml vaep-jules-j2.yml vaep-jules-j3.yml vaep-jules-j4.yml vaep-jules-j5.yml vaep-jules-j6.yml; do
    for state in queued in_progress pending waiting requested; do
      runs="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/$workflow/runs?branch=$BRANCH&status=$state&per_page=1")" || return 1
      count="$(jq -er '.total_count | numbers' <<<"$runs")" || return 1
      total=$((total + count))
    done
  done
  printf '%s\n' "$total"
}

hardening_gates_ok() {
  local head="$1" workflow runs pr
  for workflow in vaep-engine-ci.yml vaep-jules-diagnostic.yml; do
    runs="$(api "repos/$GITHUB_REPOSITORY/actions/workflows/$workflow/runs?branch=$BRANCH&head_sha=$head&per_page=10")" || return 1
    jq -e --arg head "$head" '[.workflow_runs[]? | select(.head_sha == $head and .head_branch == "Desarrollo")]
      | sort_by(.created_at, .id) | last | .status == "completed" and .conclusion == "success"' <<<"$runs" >/dev/null || return 1
  done
  pr="$(api "repos/$GITHUB_REPOSITORY/pulls/2")" || return 1
  jq -e '.state == "open" and .draft == true and .merged == false and .head.ref == "Desarrollo" and .base.ref == "main"' <<<"$pr" >/dev/null
}

next_parent() {
  python3 "$TRANSITION" select --catalog "$CATALOG"
}

publish_promotion() {
  local expected_head="$1" catalog_file="$2" admission_file="$3" parent="$4" next="$5"
  local catalog_sha admission_sha log_sha base_tree tree commit
  [[ "$(current_head)" == "$expected_head" ]] || { echo 'VAEP_PARENT_PROMOTION_DEFERRED=HEAD_CHANGED'; return 0; }
  catalog_sha="$(jq -n --rawfile content "$catalog_file" '{content:$content,encoding:"utf-8"}' | api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - --jq '.sha')"
  admission_sha="$(jq -n --rawfile content "$admission_file" '{content:$content,encoding:"utf-8"}' | api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - --jq '.sha')"
  # Preserve the complete existing changelog bytes, then append this transition.
  log_sha="$(api "repos/$GITHUB_REPOSITORY/contents/CHANGELOG_AI.md?ref=$expected_head" | python3 -c '
import base64,json,sys
record=json.load(sys.stdin)
old=base64.b64decode(record["content"])
entry=("\n\n## VAEP dependency-safe closure reconciliation\n\n"
       + "Controller: CHATGPT_BUSINESS / canonical checkpoint.\n"
       + "Validated parent: `"+sys.argv[1]+"`; successor: `"+(sys.argv[2] or "NONE")+"`.\n"
       + "Base: `"+sys.argv[3]+"`. Existing closure receipt and exact-head causal gates validated.\n"
       + "Selector uses explicit roadmap dependencies; no lexical ordering or gate bypass.\n"
       + "Admission transition is guarded; no production, merge or secret changes.\n")
json.dump({"encoding":"base64","content":base64.b64encode(old+entry.encode()).decode()},sys.stdout)
' "$parent" "$next" "$expected_head" | api "repos/$GITHUB_REPOSITORY/git/blobs" --method POST --input - --jq '.sha')"
  base_tree="$(api "repos/$GITHUB_REPOSITORY/git/commits/$expected_head" --jq '.tree.sha')"
  tree="$(jq -n --arg base "$base_tree" --arg c "$catalog_sha" --arg a "$admission_sha" --arg l "$log_sha" \
    '{base_tree:$base,tree:[{path:"vaep/control/jules-autorefill-catalog.json",mode:"100644",type:"blob",sha:$c},{path:"vaep/control/dispatch-admission.json",mode:"100644",type:"blob",sha:$a},{path:"CHANGELOG_AI.md",mode:"100644",type:"blob",sha:$l}]}' \
    | api "repos/$GITHUB_REPOSITORY/git/trees" --method POST --input - --jq '.sha')"
  commit="$(jq -n --arg tree "$tree" --arg parent "$expected_head" \
    '{message:"fix(vaep): reconcile certified closure and dependency-safe successor",tree:$tree,parents:[$parent]}' \
    | api "repos/$GITHUB_REPOSITORY/git/commits" --method POST --input - --jq '.sha')"
  [[ "$(current_head)" == "$expected_head" ]] || { echo 'VAEP_PARENT_PROMOTION_DEFERRED=HEAD_CHANGED'; return 0; }
  if jq -n --arg sha "$commit" '{sha:$sha,force:false}' | api "repos/$GITHUB_REPOSITORY/git/refs/heads/$BRANCH" --method PATCH --input - >/dev/null 2>&1; then
    # Refresh the local inputs used by the checkpoint's immediate refill.
    cp "$catalog_file" "$CATALOG"
    cp "$admission_file" "$ADMISSION"
    if [[ -n "$next" ]]; then echo "VAEP_PARENT_PROMOTED=true commit=$commit current_parent=$next";
    else echo "VAEP_PARENT_CLOSED_NO_SUCCESSOR=true commit=$commit parent=$parent"; fi
  else
    echo "VAEP_PARENT_PROMOTION_DEFERRED=REF_RACE commit=$commit"
  fi
}

main() {
  local parent next fragment head functional live now prepared
  local tmp_catalog="" tmp_admission=""
  require_runtime
  [[ -f "$MASTER_FILE" && -f "$PARSER" && -f "$CATALOG" && -f "$ADMISSION" && -f "$TRANSITION" ]] || { echo 'VAEP_CLOSE=BLOCKED reason=control_files_missing'; return 0; }
  [[ "$(bash "$PARSER" --get PARENT_CLOSE_FIRST "$MASTER_FILE")" == "TRUE" ]] || { echo 'VAEP_CLOSE=BLOCKED reason=master_parent_close_policy'; return 0; }
  head="$(current_head)"
  [[ "$(git rev-parse HEAD)" == "$head" ]] || { echo 'VAEP_CLOSE=BLOCKED reason=LOCAL_SNAPSHOT_STALE'; return 0; }
  parent="$(jq -r '.currentParent // empty' "$CATALOG")"
  fragment="$(latest_valid_fragment "$parent" || true)"
  [[ -n "$fragment" ]] || { echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=LISTO_REAL_EVIDENCE_MISSING"; return 0; }
  functional="$(functional_head "$head")" || { echo 'VAEP_CLOSE=BLOCKED reason=FUNCTIONAL_HEAD_UNPROVEN'; return 0; }
  [[ "$(jq -r '.functionalHead' "$fragment")" == "$functional" ]] || { echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=FUNCTIONAL_HEAD_MISMATCH"; return 0; }
  critical_gates_ok "$functional" "$fragment" || { echo "VAEP_CLOSE=BLOCKED current_parent=$parent reason=CAUSAL_GATES_NOT_TERMINAL_OR_SUCCESS"; return 0; }
  live="$(live_jules_runs)" || { echo 'VAEP_CLOSE=BLOCKED reason=LIVE_JULES_UNPROVEN'; return 0; }
  (( live == 0 )) || { echo "VAEP_CLOSE=BLOCKED reason=LIVE_JULES_RUNS count=$live"; return 0; }
  # Publishing control changes before their own CI finishes could cancel gates.
  hardening_gates_ok "$head" || { echo 'VAEP_CLOSE=BLOCKED reason=HARDENING_GATES_PENDING'; return 0; }
  if ! next="$(next_parent)"; then echo 'VAEP_CLOSE=BLOCKED reason=ROADMAP_DEPENDENCIES_NOT_READY'; return 0; fi
  if [[ -z "$next" ]] && jq -e --arg p "$parent" --arg f "$fragment" '.closureReceipts[$p] == $f' "$CATALOG" >/dev/null; then
    echo "VAEP_PARENT_CLOSED=true parent=$parent promotion=NEXT_PARENT_MISSING"; return 0
  fi
  now="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  prepared="$(python3 "$TRANSITION" prepare --catalog "$CATALOG" --admission "$ADMISSION" --receipt "$fragment" --functional "$functional" --now "$now" --hardening-ok)" || { echo 'VAEP_CLOSE=BLOCKED reason=TRANSITION_INVALID'; return 0; }
  tmp_catalog="$(mktemp)"; tmp_admission="$(mktemp)"
  trap 'rm -f "${tmp_catalog:-}" "${tmp_admission:-}"' EXIT
  jq '.catalog' <<<"$prepared" > "$tmp_catalog"
  jq '.admission' <<<"$prepared" > "$tmp_admission"
  publish_promotion "$head" "$tmp_catalog" "$tmp_admission" "$parent" "$next"
}

if [[ "${1:-}" == "--self-test" ]]; then
  [[ -f "$MASTER_FILE" && -f "$PARSER" && -f "$TRANSITION" ]] || exit 2
  for control_path in AGENTS.md README.md CHANGELOG_AI.md PLAN_EJECUCION_AUTONOMA.md PROJECT_CONTEXT.md docs/VAEP_AUTHORITY.md docs/CONTEXTO_CHATGPT_VAEP.md docs/N4.11_CENTROS_COSTO_FRONTEND.md vaep/control/dispatch-admission.json scripts/vaep/terminal_handoff.py .github/workflows/vaep-checkpoints.yml; do
    is_control_plane_path "$control_path" || exit 2
  done
  if is_control_plane_path backend/src/API/Program.cs; then exit 2; fi
  python3 -m unittest discover -s scripts/vaep -p 'test_parent*.py'
  echo 'VAEP_PARENT_CLOSE_SELF_TEST=PASS'
  exit 0
fi
main "$@"
