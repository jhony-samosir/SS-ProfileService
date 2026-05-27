# Integration with SS-AuthService

## Overview
SS-ProfileService needs to integrate with SS-AuthService to automatically provision user profiles upon user registration and verification.

## Event-Driven Architecture
We will use an event-driven choreography pattern. When a user registers or verifies their email, SS-AuthService publishes a message to RabbitMQ, which SS-ProfileService consumes to create a base profile.

## Events to Consume

### UserRegistered
- **Routing Key:** `auth.user.registered`
- **Exchange:** `samstore.events` (Topic)
- **Payload:**
  ```json
  {
    "userId": 123,
    "publicId": "guid-here",
    "email": "user@example.com",
    "fullName": "Full Name"
  }
  ```

### UserVerified
- **Routing Key:** `auth.user.verified`
- **Exchange:** `samstore.events` (Topic)
- **Payload:**
  ```json
  {
    "userId": 123,
    "publicId": "guid-here",
    "email": "user@example.com"
  }
  ```

## Integration Flow

```mermaid
sequenceDiagram
    participant Auth as SS-AuthService
    participant RB as RabbitMQ<br/>(samstore.events)
    participant Profile as SS-ProfileService
    participant AuthDB as Auth DB<br/>(PostgreSQL)
    participant ProfileDB as Profile DB<br/>(PostgreSQL)

    %% User Registration Flow
    Auth->>AuthDB: 1. Create user (txn begins)
    AuthDB-->>Auth: 2. User record saved
    Auth->>AuthDB: 3. Insert outbox event:<br/>EventType='UserRegistered',<br/>Payload={userId, publicId, email, fullName}
    AuthDB-->>Auth: 4. Outbox event saved (txn commits)
    Auth->>RB: 5. OutboxWorker publishes:<br/>RoutingKey='auth.user.registered',<br/>MessageId='auth-event-123',<br/>CorrelationId='123'
    RB-->>Auth: 6. Message published (confirms)
    Auth->>RB: 7. Outbox event status='published'
    RB->>Profile: 8. Deliver to queue:<br/>ss-profile-service.user-events
    Profile->>ProfileDB: 9. Check InboxEvents<br/>WHERE MessageId='auth-event-123'
    alt Message NOT found
        Profile->>ProfileDB: 10. Check UserProfiles<br/>WHERE UserId=123
        alt Profile NOT exists
            Profile->>ProfileDB: 11. Insert UserProfile<br/>+ Insert InboxEvent<br/>(txn begins)
            ProfileDB-->>Profile: 12. Records saved
            Profile->>RB: 13. Ack message
        else Profile exists
            Profile->>ProfileDB: 14. Insert InboxEvent only<br/>(txn begins)
            ProfileDB-->>Profile: 15. InboxEvent saved
            Profile->>RB: 16. Ack message
        end
    else Message found (duplicate)
        Profile->>RB: 17. Ack message (skip)
    end
```

## Implementation Plan

### 1. RabbitMQ Subscriber
Create a background worker (`UserEventConsumerWorker` inheriting from `BackgroundService`) in SS-ProfileService to subscribe to `samstore.events` and bind a private queue (e.g., `ss-profile-service.auth-events`).

### 2. Idempotent Processing (Inbox Pattern)
To prevent duplicate processing due to RabbitMQ redelivery, implement the **Inbox Pattern** using the existing `InboxEvents` table in the database:
- On event receipt, extract the message ID.
- Begin an ACID transaction with the DbContext.
- Check if the message ID exists in the `InboxEvents` table.
- If it exists, acknowledge the message and skip processing.
- If it does not exist, insert the `InboxEvent` record marking it as `PROCESSED`.
- Check if a profile already exists for the given `UserId`. If not, create a base `UserProfile` using the `fullName`, `userId`, and `userPublicId` from the payload.
- Commit the transaction and acknowledge (ACK) the message to RabbitMQ.

## Configuration Requirements
Add the following to `appsettings.json` in `SS.ProfileService.API`:
```json
{
  "RabbitMQ": {
    "Host": "host.docker.internal",
    "Port": 5672,
    "QueueName": "ss-profile-service.auth-events"
  }
}
```