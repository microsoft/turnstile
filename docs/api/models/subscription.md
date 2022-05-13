# Subscription

Represents a Turnstile-managed subscription.

## Example

```json
{
  "subscription_id": "20bece32-0256-4700-81b9-4fa12ddc4bfb",
  "subscription_name": "Example subscription",
  "tenant_id": "203ed2f8-3378-474f-8604-bf6f472ddca3",
  "tenant_name": "Example tenant",
  "offer_id": "Example offer",
  "plan_id": "Example plan"
  "state": "active",
  "admin_role_name": "Subscription administrators",
  "user_role_name": "Subscription users",
  "management_urls": {
    "Management link 1": "https://...", 
    "Management link 2": "https://..."
  },
  "admin_name": "Subscription administrator",
  "admin_email": "subscription_admin@...com",
  "total_seats": 100,
  "is_being_configured": false,
  "is_free_trial": false,
  "is_setup_complete": false,
  "is_test_subscription": false,
  "created_utc": "2022-01-01T00:00:00Z",
  "state_last_updated_utc": "2022-02-02T00:00:00Z",
  "seating_config": {
    ...
  },
  "subscriber_info": {
    ...
  },
  "source_subscription": {
    ...
  }
}
```
