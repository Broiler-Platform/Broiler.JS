#!/usr/bin/env bash
# ci-commit-generated-results.sh — commit regenerated result files back to a
# long-running job's branch, tolerating pushes that land while the job runs.
#
# The test262 workflow can shard for up to six hours before committing its
# generated failure manifest to the branch it was dispatched from. A plain
# `git push` there is a race the job can lose on a busy day: any intervening
# push moves the branch and rejects the generated-results commit as a
# non-fast-forward update, discarding hours of measurement.
#
#     ! [rejected]        HEAD -> main (fetch first)
#     error: failed to push some refs to '...'
#
# By default this script rebuilds its commit on top of whatever the branch tip
# has become and pushes again, up to --attempts times. Workflows whose results
# are valid only for one tested source revision should pass --expected-base;
# then the push is compare-and-swap and stale measurements are never reparented.
#
# The rebuild is a wholesale replacement of the given paths rather than a rebase:
# these files are *generated*, always written in full by the run that produced
# them, so a three-way merge of two runs' output has no meaning and would only
# invent conflicts. Whoever measured last wins the result files; everything else
# the branch gained in the meantime is preserved by construction, because the
# fresh remote tip is the commit's new parent.
#
# Usage:
#     ./scripts/ci-commit-generated-results.sh --branch <branch> \
#         [--expected-base <commit>] --message <commit message> \
#         <path> [<path>...]
#
# Options:
#     --branch <name>    Branch to push to (required).
#     --message <text>   Commit message (required).
#     --expected-base    Only write while the remote branch still equals this
#                        commit; safely skip if it moved (optional).
#     --attempts <n>     Push attempts before giving up (default 5).
#     --delay <sec>      Initial backoff between attempts, doubled each
#                        time (default 5).
#
# Environment:
#     GIT_AUTHOR_IDENTITY_NAME / _EMAIL   Override the committing identity
#                                         (default: github-actions[bot]).
#
# Exits 0 when the results are on the remote branch, or when there was nothing
# to commit. Exits non-zero — loudly — when every attempt lost the race, rather
# than leaving a green job with no results committed.

set -euo pipefail

BRANCH=""
MESSAGE=""
EXPECTED_BASE=""
ATTEMPTS=5
DELAY=5
PATHS=()

die() {
    echo "ci-commit-generated-results: $*" >&2
    exit 2
}

while [ $# -gt 0 ]; do
    case "$1" in
        --branch)   BRANCH="${2:-}"; shift 2 ;;
        --message)  MESSAGE="${2:-}"; shift 2 ;;
        --expected-base) EXPECTED_BASE="${2:-}"; shift 2 ;;
        --attempts) ATTEMPTS="${2:-}"; shift 2 ;;
        --delay)    DELAY="${2:-}"; shift 2 ;;
        -h|--help)  sed -n '2,45p' "$0"; exit 0 ;;
        --)         shift; PATHS+=("$@"); break ;;
        -*)         die "unknown option: $1" ;;
        *)          PATHS+=("$1"); shift ;;
    esac
done

[ -n "$BRANCH" ]  || die "--branch is required"
[ -n "$MESSAGE" ] || die "--message is required"
[ "${#PATHS[@]}" -gt 0 ] || die "at least one path is required"

# Trailing slashes are natural to write for a results directory but break
# `git cat-file -e <rev>:<path>` below, so normalize them away once here.
for i in "${!PATHS[@]}"; do
    PATHS[$i]="${PATHS[$i]%/}"
done

git config user.name  "${GIT_AUTHOR_IDENTITY_NAME:-github-actions[bot]}"
git config user.email "${GIT_AUTHOR_IDENTITY_EMAIL:-41898282+github-actions[bot]@users.noreply.github.com}"

skip_stale_results() {
    local actual="$1"
    echo "Skipping generated-result persistence: $BRANCH moved from tested base $EXPECTED_BASE to $actual." >&2
    if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
        {
            echo "### Generated results were not persisted"
            echo
            echo "The branch moved after measurement (expected \`$EXPECTED_BASE\`, found \`$actual\`), so stale results were not applied."
        } >> "$GITHUB_STEP_SUMMARY"
    fi
    exit 0
}

if [ -n "$EXPECTED_BASE" ]; then
    EXPECTED_BASE="$(git rev-parse "$EXPECTED_BASE^{commit}")" \
        || die "--expected-base is not a commit: $EXPECTED_BASE"
    CURRENT_HEAD="$(git rev-parse HEAD)"
    [ "$CURRENT_HEAD" = "$EXPECTED_BASE" ] \
        || die "working tree HEAD $CURRENT_HEAD does not match --expected-base $EXPECTED_BASE"
    git fetch origin "$BRANCH"
    REMOTE_HEAD="$(git rev-parse FETCH_HEAD)"
    if [ "$REMOTE_HEAD" != "$EXPECTED_BASE" ]; then
        skip_stale_results "$REMOTE_HEAD"
    fi
fi

# Stage first, then test the staged diff: `git diff` ignores untracked files, so
# checking before `git add` would never detect a result file's first-ever
# creation (it is untracked until then).
stage() {
    git add -A -- "${PATHS[@]}"
}

# True when the index differs from HEAD for the given paths — i.e. there is
# something worth committing.
staged_changes() {
    ! git diff --cached --quiet -- "${PATHS[@]}"
}

stage
if ! staged_changes; then
    echo "No result changes to commit."
    exit 0
fi

git commit -m "$MESSAGE"

# The content to keep across every rebuild attempt. Kept as a commit id rather
# than a copy on disk: it stays reachable through the reflog after the resets
# below, so `git checkout $RESULTS -- <path>` restores exactly what was measured.
RESULTS="$(git rev-parse HEAD)"

attempt=1
delay="$DELAY"
while :; do
    if git push origin "HEAD:$BRANCH"; then
        echo "Pushed results to $BRANCH (attempt $attempt/$ATTEMPTS)."
        exit 0
    fi

    if [ "$attempt" -ge "$ATTEMPTS" ]; then
        die "push to $BRANCH still rejected after $ATTEMPTS attempts; results were not committed"
    fi

    echo "Push to $BRANCH was rejected — the branch moved while this job ran." >&2
    echo "Rebuilding the results commit on the current tip (attempt $((attempt + 1))/$ATTEMPTS in ${delay}s)." >&2
    sleep "$delay"
    delay=$((delay * 2))
    attempt=$((attempt + 1))

    git fetch origin "$BRANCH"

    if [ -n "$EXPECTED_BASE" ]; then
        REMOTE_HEAD="$(git rev-parse FETCH_HEAD)"
        if [ "$REMOTE_HEAD" != "$EXPECTED_BASE" ]; then
            skip_stale_results "$REMOTE_HEAD"
        fi
        # The remote did not move; this was a transient push failure. Keep the
        # same compare-and-swap commit and retry without rebasing it.
        continue
    fi

    # Start again from the branch tip as it is *now*. --hard also clears any
    # working-tree dirt the benchmark left behind in tracked files, which would
    # otherwise block the switch; the measured output is safe in $RESULTS.
    git reset --hard FETCH_HEAD

    # Replace the generated paths wholesale: drop whatever the tip carries for
    # them, then restore this run's output. Dropping first is what makes a file
    # this run no longer produces actually disappear.
    for path in "${PATHS[@]}"; do
        git rm -rq --ignore-unmatch -- "$path"
        if git cat-file -e "$RESULTS:$path" 2>/dev/null; then
            git checkout "$RESULTS" -- "$path"
        fi
    done

    stage
    if ! staged_changes; then
        # Another run of this same job committed identical results while we were
        # losing the race. Nothing left to say.
        echo "Branch $BRANCH already carries these results."
        exit 0
    fi

    git commit -m "$MESSAGE"
    RESULTS="$(git rev-parse HEAD)"
done
