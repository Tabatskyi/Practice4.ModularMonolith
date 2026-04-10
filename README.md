# Practice4.ModularMonolith

Theme: Student Marketplace

Core Item: Listing

Domain rules:
- A listing title cannot be empty
- A listing price must be a positive number
- Status transitions are limited to: 
  - Draft -> Published
  - Published -> Sold
  - Published -> Draft
  - Draft -> Deleted
- A listing cannot be edited after it is sold

# Practice5.Microservices

Run:
`docker compose up -d`

Gateway base URL:
`http://localhost`

## service URLs
Core Service: `http://localhost:5000`
Users Service: `http://localhost:5001`
swagger enabled for both services at root

## curl Tests

1. Create user

```bash
curl -i -X POST http://localhost/users -H "Content-Type: application/json" -d '{"displayName":"Vasenka"}'
```

Expected result:
- Status: `201 Created`
- Response contains `userId`

2. Create core item for existing user (success)

Use the `userId` from step 1.

```bash
curl -i -X POST http://localhost/core/core-items -H "Content-Type: application/json" -d '{"title":"RTX 4080","price":2500, "ownerUserId":"<userId>"}'
```

Expected result:
- Status: `201 Created`
- Response contains created listing id

3. Create core item for non-existing user (fails)

```bash
curl -i -X POST http://localhost/core/core-items -H "Content-Type: application/json" -d '{"title":"Invalid owner test","price":1000,"ownerUserId":"11111111-1111-1111-1111-111111111111"}'
```

Expected result:
- Status: `400 Bad Request`
- Response contains an error about non-existing user
