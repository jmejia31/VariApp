# Throughput recovery guardrails

- Resolve CURRENT_PARENT and CURRENT_HEAD live at the beginning and again before any closure/promotion.
- Never treat workflow IN_PROGRESS alone as ACTIVE_REAL; require correlated Jules session + useful activity.
- Conversely, never classify a session as stalled while correlated useful activity is fresher than 10 minutes.
- Reconcile ownership before any takeover. If an owner is live, do not create a second writer.
- Terminal Jules result with PATCH_PRESENT and valid contract must enter REVIEW_FIRST immediately.
- Certifiable parent must close/promote/refill in the same control cycle.
- Prearm roadmap-derived N+1/N+2 scopes dependency-gated to avoid post-promotion starvation.
- Throughput target does not authorize filler or false closures.
