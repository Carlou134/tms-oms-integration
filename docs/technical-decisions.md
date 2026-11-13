# Technical Decisions - TMS-OMS Integration

This document explains **why** the integration is built the way it is. It covers the **pattern
justification** deliverable and records the interpretations of the requirements, the assumptions
and the known limitations.

The architecture and its components are described in [diagram.md](diagram.md).

1. [Pattern justification](#1-pattern-justification)
2. [Interpretations of the requirements](#2-interpretations-of-the-requirements)
3. [Assumptions](#3-assumptions)
4. [Known limitations](#4-known-limitations)

---

## 1. Pattern justification

### Summary

| Pattern | Type | Requirements |
|---|---|---|
| [Webhook](#11-webhook) | Integration | 1 |
| [Idempotent Receiver](#12-idempotent-receiver) | Integration | 1, 3, 8 |
| [Envelope Wrapper](#13-envelope-wrapper) | Integration | 1 |
| [Message Translator](#14-message-translator) | Integration | 1, 7 |
| [Publish-Subscribe Channel](#15-publish-subscribe-channel) | Integration | 5, 6, 7, 8 |
| [Message Filter](#16-message-filter) | Integration | 6 |
| [Retry with exponential backoff](#17-retry-with-exponential-backoff) | Integration | 8 |
| [Dead Letter Channel](#18-dead-letter-channel) | Integration | 8 |
| [Transactional Outbox](#19-transactional-outbox) | Integration | 8 |
| [Clean Architecture](#110-clean-architecture) | Design | All |
| [Aggregate and Domain Events](#111-aggregate-and-domain-events) | Design | 2, 3, 4, 5 |
| [Strategy](#112-strategy) | Design | 7 |

Integration pattern names follow the *Enterprise Integration Patterns* catalog (Hohpe and Woolf).

### Integration patterns

#### 1.1 Webhook

- **Requirement:** 1. Receive TMS events in real time.
- **Problem:** the integration needs to know about each status change as soon as it happens.
- **Solution:** the TMS sends a `POST` to `/api/webhooks/tms/events` every time an event occurs. The API authenticates, validates, discards duplicates, publishes and responds `202 Accepted` without running the business logic.
- **Why this pattern:** the TMS notifies only when there is something new, with no wasted queries or delays.
- **Discarded alternative:** querying the TMS periodically (polling). With thousands of daily shipments, that would mean thousands of queries with no result, and every change would be detected late.
- **Tradeoff:** the integration does not control when, how many times or in what order events arrive. That's why the endpoint responds quickly (to keep the TMS from resending on timeout), requires an API key and discards duplicates.

#### 1.2 Idempotent Receiver

- **Requirements:** 1 (reception), 3 (visit counter) and 8 (consistency).
- **Problem:** webhooks are delivered *at least once*: if the TMS does not receive the response, it resends the same event. A duplicated `NOT DELIVERED` would add an extra visit, and could trigger a `TO BE RETURN` with only two real visits.
- **Solution:** since the payload does not include an event identifier, a SHA-256 key is computed from `orderNumber`, `serviceType`, `status`, `subStatus` and `eventDate`. `TryRegisterAsync` registers the key in a single atomic operation: if it already existed, the event is discarded and `202` is returned anyway. If publishing fails after registering the key, the key is released so the TMS resend is not discarded by mistake.
- **Why this pattern:** the requirements don't mention duplicates, but webhook resends and the retries from requirement 8 produce them. Without deduplication, requirement 3 gets corrupted.
- **Discarded alternative:** checking whether the key exists and then adding it, in two steps. Two simultaneous resends would both pass the check (race condition).
- **Tradeoff:** two different events with exactly the same fields would be treated as duplicates. This is unlikely, because `eventDate` has second-level precision.

#### 1.3 Envelope Wrapper

- **Requirement:** 1.
- **Problem:** subscribers need technical metadata (the event key, the reception time) that is not part of the TMS payload.
- **Solution:** `TmsEventReceived` wraps the business event (`TmsEvent`) together with `EventKey` and `ReceivedAt`.
- **Why this pattern:** it separates business data from technical data. The key is computed once and reused by every subscriber.
- **Discarded alternative:** adding the metadata as fields of `TmsEvent`, mixing what the TMS says with what the integration adds.

#### 1.4 Message Translator

- **Requirements:** 1 (input) and 7 (output).
- **Problem:** the TMS uses its own values (for example, `"AT PICKUP POINT"`, with spaces), and each customer expects notifications in its own structure.
- **Solution:** on input, `TmsCodes` translates TMS values into domain enums. On output, each formatter translates the neutral `OrderStatusNotification` model into its customer's structure.
- **Why this pattern:** the domain knows neither the TMS format nor any customer's format. A format change affects a single class.
- **Discarded alternative:** putting serialization attributes on the domain enums, which would make the innermost layer depend on an external system's JSON format.

#### 1.5 Publish-Subscribe Channel

- **Requirements:** 5 (history), 6 (evidences), 7 (notification) and 8 (independent retries).
- **Problem:** a single event triggers three independent effects. If one fails or is slow, the others should not be affected.
- **Solution:** a bus with two topics: received events and processed events. Each subscriber has its own channel and its own worker, with its own retries (*fan-out*). Subscriptions are registered at application startup.
- **Why this pattern:** a slow photo download does not delay the history, and a failed notification is retried without repeating the others. It's choreography: each subscriber reacts on its own.
- **Discarded alternatives:**
  - **Synchronous processing in the webhook:** the response to the TMS would depend on the slowest step, and a failure in any step would fail the whole event.
  - **In-memory Observer:** it's synchronous, shares the thread and doesn't allow retrying a single subscriber.
  - **Orchestration (Saga):** the effects don't depend on each other nor need compensation, so a central orchestrator would be complexity with no benefit.
  - **A single shared channel:** each message would reach only one subscriber (*Competing Consumers*), not all of them.
- **Tradeoff:** side effects are eventually consistent: they happen a few milliseconds after the order is updated.

#### 1.6 Message Filter

- **Requirement:** 6. Evidences are stored only at six milestones.
- **Problem:** every processed event reaches the evidence subscriber, but only some should store evidences.
- **Solution:** `EvidenceMilestoneFilter` lets an event through only if its status is one of the six milestones (`COLLECTED`, `NOT COLLECTED`, `DELIVERED`, `NOT DELIVERED`, `RETURNED`, `NOT RETURNED`), if it carries evidences and if it was applied to the order.
- **Why this pattern:** the requirement's rule lives in a single place, separate from downloading and storage.
- **Discarded alternative:** *Content-Based Router*. A router picks among several destinations based on content; here there is a single destination and the decision is pass or don't pass.

#### 1.7 Retry with exponential backoff

- **Requirement:** 8. Automatically handle transient failures.
- **Problem:** a momentary network or evidence server outage should not lose data or require manual intervention.
- **Solution:** Polly at two levels:

  | Level | Attempts | Delays | Retries on |
  |---|---|---|---|
  | HTTP evidence download | 4 | ~2s, ~4s, ~8s | 5xx, 408, 429, network errors and per-attempt timeouts (10s). Does not retry 4xx |
  | Each bus subscriber | 4 | ~1s, ~2s, ~4s | Any error, except application shutdown |

  Each subscriber retry uses a new dependency injection scope, so no state is carried over from a failed attempt.
- **Why this pattern:** exponential backoff gives the service time to recover, and *jitter* spreads out retries so many simultaneous failures don't retry at the same instant.
- **Discarded alternative:** fixed-delay retries, which keep hammering a service that is already struggling.
- **Tradeoff:** **transient** errors are distinguished from **permanent** ones. A malformed URL (`htps://`) or a 404 won't be fixed by retrying, so they are not retried at the HTTP level.

#### 1.8 Dead Letter Channel

- **Requirement:** 8. Prevent data loss.
- **Problem:** a message that exhausts all retries would be lost.
- **Solution:** the worker stores it in a dead letter queue with the full message as JSON, the failing subscriber, the error and the date. If storing it also fails, the worker logs it as a critical error and keeps processing.
- **Why this pattern:** the message remains available to inspect or reprocess. Since the subscriber is recorded, only the failed effect can be reprocessed, without repeating the others.

#### 1.9 Transactional Outbox

- **Requirement:** 8. Guarantee consistency.
- **Problem:** saving the order and publishing its events are two operations. If publishing fails after saving, the order changes but the history, evidences and notification never find out.
- **Solution:** the handler saves the order and the Outbox message in the same unit of work. A background `OutboxDispatcher` publishes pending messages and only removes them after publishing.
- **Why this pattern:** a publishing failure leaves the message pending for the next attempt. Delivery is *at least once*: there may be a duplicate, but never a loss.
- **Discarded alternative:** a distributed transaction between the database and the broker, which most brokers don't support and which adds complexity and coupling.

### Design patterns

#### 1.10 Clean Architecture

- **Problem:** the requirements allow replacing external services with in-memory implementations, but the design must be able to evolve to real services.
- **Solution:** four projects with dependencies pointing toward the center: Domain depends on nothing, Application defines the **ports** (interfaces) and Infrastructure **implements** them.
- **Why this pattern:** replacing the in-memory bus with a real broker, or the repositories with a database, only modifies Infrastructure. Business rules and use cases don't change.

#### 1.11 Aggregate and Domain Events

- **Requirements:** 2, 3, 4 and 5.
- **Problem:** the final state, the visit counter and the automatic `TO BE RETURN` are rules that must always hold, no matter who modifies the order.
- **Solution:** `Order` is an aggregate with read-only properties and a single method, `Apply`, to change its state. Instead of calling other components, it records what happened as domain events (`OrderStatusChanged` and `OrderEventRejected`) that are published afterwards.
- **Why this pattern:** the rules can't be bypassed, and the domain knows nothing about history, evidences or notifications.
- **Discarded alternative:** Observer inside the domain. The aggregate has no subscribers: it only accumulates facts that are published after saving.

#### 1.12 Strategy

- **Requirement:** 7. Each customer has its own notification structure.
- **Solution:** each customer with its own format has an `INotificationFormatter`, and a default formatter covers the rest. `NotificationFormatterResolver` picks the formatter based on the customer code.
- **Why this pattern:** adding a customer means adding a class and registering it. The handler and the resolver don't change (open/closed principle).
- **Discarded alternative:** a `switch` on the customer code inside the handler, which would grow with every new customer.

---

## 2. Interpretations of the requirements

| Topic | Interpretation | Reason |
|---|---|---|
| Final states | Only `DELIVERED` and `RETURNED` block new events. A full transition table is not validated | That's what requirement 2 asks for. A strict table could reject real TMS events, for example if a `STARTED` is lost |
| Visit counter | Only `DELIVERED` and `NOT DELIVERED` add a visit | They are the only states in which the courier reached the address |
| Automatic return | `TO BE RETURN` is emitted when the third visit is a `NOT DELIVERED`. If it's a `DELIVERED`, the order is already final | Returning a delivered order contradicts requirement 2 |
| Timing of the return | The automatic `TO BE RETURN` is applied in the same operation as the third `NOT DELIVERED`, and is flagged as automatic | An order can never be left with three failed visits without moving to return |
| Return phase | `TO BE RETURN` belongs to the return phase | "Assigned to return" is already part of that process |
| History | Rejected events are also recorded, with their reason | Requirement 5 asks to record *every* event |
| Evidences | Evidences from rejected events are not stored | They don't correspond to any change in the order. The rejection is still recorded in the history |
| Notification | Rejected events are not notified. The third failed visit produces two notifications: `NOT DELIVERED` and `TO BE RETURN` | For the customer, a rejected event changes nothing |
| Automatic data | The automatic `TO BE RETURN` does not inherit `subStatus`, courier, vehicle or receiver from the event that triggered it | No courier reported it |
| Sub-statuses | `subStatus` is accepted as free text | The requirements don't define its possible values |
| Unknown orders | An event for an order that doesn't exist in the OMS is ignored and logged | The OMS is the source of truth for orders; the integration does not create them |

---

## 3. Assumptions

| Topic | Assumption |
|---|---|
| Event date | `eventDate` arrives in `yyyy-MM-dd HH:mm:ss` format, without a time zone, and is in Lima time (UTC-5). A fixed offset is used, because Peru has no daylight saving time |
| Required fields | `serviceType`, `status`, `details.orderNumber` and `eventDate`. Everything else is optional |
| TMS values | `status`, `serviceType` and `dispatchType` are compared case-insensitively |
| Event identification | The combination of order, service type, status, sub-status and date identifies a single event |
| Webhook authentication | The TMS sends an API key in the `X-Api-Key` header. If the TMS signs its payloads (for example, with HMAC), validating the signature would be more secure |
| Order customer | The customer code is taken from the order in the OMS; the one in the TMS payload is used only if the OMS doesn't have it |
| Duplicate response | The TMS stops resending an event when it receives a `2xx` |

---

## 4. Known limitations

| Topic | Limitation | Possible improvement |
|---|---|---|
| In-memory persistence | Orders, history, evidences, pending messages and keys are lost on restart | Real database and external services |
| Outbox atomicity | In memory, it can't be shown that the order and the message are saved in the same transaction | Outbox table in the same database as the orders |
| Idempotency keys | They don't expire, so memory grows with each event | Distributed cache with expiration |
| Duplicates | *At-least-once* delivery may repeat a notification. In memory, retrying `ProcessTmsEventHandler` could apply an event twice, because the repository returns the same instance | Record the keys of already applied events on the order |
| Out-of-order events | The date of the last event is stored, but older events are not discarded | Ignore events whose `eventDate` is earlier than the last applied one |
| Duplicate `TO BE RETURN` | If the TMS sends its own `TO BE RETURN` after the automatic one, it is applied and notified again | Ignore a change to the same status |
| Multiplied retries | In the worst case, a download is attempted 16 times (4 HTTP × 4 subscriber), and the subscriber also retries permanent errors | Exclude 4xx errors from the subscriber retry |
| Evidence size | There is no download size limit | Reject large files based on `Content-Length` |
| History query | The endpoint has no authentication | Its own authentication, for example with JWT |
| Notification formats | Defined in code | Per-customer templates as configuration |
| Dead letter queue | There is no endpoint to query it or reprocess its messages | Reprocessing from the broker or an administrative endpoint |
