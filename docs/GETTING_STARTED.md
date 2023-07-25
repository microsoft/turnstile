# SaaS Seating with Turnstile in _x_ Easy Steps

Turnstile makes it easier to build SaaS apps on Azure by automating the process of providing seats (or licenses, badges, etc.) to your users. It's easy to set up (as you're about to learn firsthand) and cost-effective allowing you to scale dynamically to meet your customer's frequently changing needs. Turnstile is designed to support any Azure-based SaaS app regardless of development stack or architecture. 

## Before we get started

First, ensure that the following prerequisites are met.

* You have an active Azure Subscription. [If you don't already have one, get one free here.](https://azure.microsoft.com/free)
* You can create new Azure Active Directory app registrations. In order to create app registrations, you must be a directory administrator. For more information, see [this article](https://docs.microsoft.com/azure/active-directory/roles/permissions-reference).
* You can create resources and resource groups within the target Azure subscription. Typically, this requires at least [contributor-level access](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#contributor) to the subscription.
* You're using an Azure Active Directory work or school account. Guest or personal accounts won't work.

## 1. Clone the Turnstile GitHub repo

Navigate to [the Azure portal](https://portal.azure.com) and [open the __Bash__ Cloud Shell](https://learn.microsoft.com/en-us/azure/cloud-shell/quickstart?tabs=azurecli). Clone the Turnstile repo by running the following command in the cloud shell:

```shell
git clone https://github.com/microsoft/turnstile
```

## 2. Run the Turnstile setup script

Navigate to the newly cloned Tursntile repo's setup folder by running the following command in the cloud shell:

```shell
cd ./turnstile/Turnstile/Turnstile.Setup
```

Allow the setup script to be executed locally by running the following command in the cloud shell:

```shell
chmod +x ./setup_turnstile.sh
```

Now it's time to actually run the setup script. You'll need to provide a few parameters:

* __Deployment name (`-n`).__ All Turnstile URLs, supporting Azure resources, and Azure Active Directory app registrations will include this name by default. Deployment name must be globally unique, alphanumeric (containing only letters and numbers), and between 5-13 characters in length.
* __Deployment region (`-r`).__ [Azure is available in more than 60 regions around the globe.](https://azure.microsoft.com/explore/global-infrastructure/geographies/#overview) For a complete listing, run `az account list-locations -o table` from the cloud shell. Be sure to use the region's `Name`, not `DisplayName` or `RegionalDisplayName`.
* Learn about additional optional parameters by running `./setup_turnstile.sh -h` from the cloud shell.

Assuming your deployment name is `dontusethis` and your region is `southcentralus` (South Central US), run the following command in the cloud shell:

```shell
./setup_turnstile.sh -n "dontusethis" -r "southcentralus"
```
This will take about 10 minutes so take a moment to freshen your coffee. ☕

### The deployment summary

Once the script is finished, it will provide you with a `Turnstile deployment summary`. Make note of these specific values because you'll need them again here in a few moments:

| Doc reference | Deployment summary label | Description |
| --- | --- | --- |
| `[deployment_name]` | `Deployment name` | Your Turnstile deployment's unique name as provided in the `-n` setup script parameter |
| `[subscription_id]` | `Deployed in Azure subscription` | The Azure subscription in which Turnstile has just been deployed |
| `[resource_group]` | `Deployment in resource group` | The Azure resource group in which Turnstile has just been deployed |
| `[azure_ad_tenant_id]` | `Azure AD tenant ID` | The Azure Active Directory tenant you used to set up Turnstile |
| `[api_base_url]` | `Turnstile API base URL` | The base URL of your Turnstile API |
| `[api_key]` | `Turnstile API key (secret!)` | The API key to be used when calling your Turnstile API |
| `[user_web_app_base_url]` | `User web app base URL` | The base URL of the user (customer) web app |
| `[admin_web_app_base_url]` | `Admin web app base URL` | The base URL of the admin (publisher) web app |
| `[base_storage_url]` | `Storage account base URL` | The base URL of the storage account from which seat information can be obtained |

> __Note__: Setting the `-e` setup script flag will automatically output these values to `./[deployment_name].turnstile.env`. Handle this file with extreme care as it contains sensitive information like the Turnstile API key.

### Finish setting up Turnstile

The script will also prompt you to finish setting up Turnstile by navigating to a setup page deployed within your Azure subscription. The URL will look like this:

```url
https://turn-admin-[deployment_name].azurewebsites.net/config/basics
```

Click the link and tell Turnstile a little about your company and app so it can better tailor your user's experience.

![Set up Turnstile](images/Setup%20up%20Turnstile.png)

> You can return to this setup screen at any time by navigating to `[admin_web_app_base_url]/config/basics`.

## 3. Configure event integrations
Turnstile publishes a variety of subscription and seat-related events designed to make it easier to integrate with your existing apps and services. By default, Turnstile also publishes a set of template Logic Apps preconfigured to handle each event type. [The Logic Apps platform provides hundreds of prebuilt connectors so you can connect and integrate apps, data, services, and systems more easily and quickly.](https://learn.microsoft.com/azure/connectors/introduction) You can focus more on designing and implementing your solution's business logic and functionality, not on figuring out how to access your resources. These logic apps can be found in the same resource group in which Turnstile was deployed. You can find the name of this resource group in the setup script's `Tursntile deployment summary`.

### Turnstile events

| Event | Logic app name | Notes
| --- | --- | -- |
| Subscription created | `turn-on-subscription-created-[deployment_name]` | 
| Subscription updated | `turn-on-subscription-updated-[deployment_name]` |
| Admission granted | `turn-on-admission-granted-[deployment_name]` | Occurs when a user is granted access to the app |
| Admission denied | `turn-on-admission-denied-[deployment_name]` | Occurs when a user is denied access to the app. This event includes a code which indicates why the user was denied access. |
| Seat reserved | `turn-on-seat-reserved-[deployment_name]` | This event includes a link that the user can use to redeem their seat. Configure this logic app to notify the user that they've been invited to use the app. |
| Seat redeemed | `turn-on-seat-redeemed-[deployment_name]` | Occurs when a reserved seat is redeemed by its user |
| Seat provided | `turn-on-seat-provided-[deployment_name]` | Occurs when a dynamic seat (not reserved) is provided to a user |
| Reduced subscription seating available |  `turn-on-low-seat-warning-[deployment_name]` | Occurs when a user-based subscription (fixed seat count) reaches <= 25% available standard seating |
| No more seats available | `turn-on-no-seats-available-[deployment_name]` | Occurs when a user-based subscription (fixed seat count) runs out of available standard seating

## 4. Create a subscription

Turnstile needs to know about the SaaS subscriptions that it will be providing seats for. New subscriptions are posted to Turnstile's subscriptions API endpoint. We'll create a new subscription now to understand better how the subscriptions API works.

Using your favorite API client (Postman, cURL, etc.), POST the following JSON object to `[api_base_url]/subscriptions/085a0ed4-84e0-43f7-a601-461ea81667a1?code=[api_key]`:

```json
{
  "subscription_id": "085a0ed4-84e0-43f7-a601-461ea81667a1",
  "subscription_name": "My first subscription!",
  "tenant_id": "[azure_ad_tenant_id]",
  "tenant_name": "Your company's name",
  "total_seats": 5,
  "offer_id": "Offer 1",
  "plan_id": "Plan A",
  "state": "active",
  "is_free_trial": true,
  "is_test_subscription": true,
  "seating_config": {
    "limited_overflow_seating_enabled": true
  }
}
```

The API should return `200 OK`. If not, review your `[api_base_url]`, `[api_key]`, and JSON payload then try again. If you're still encountering issues, please let us know by [creating a new issue](https://github.com/microsoft/turnstile/issues/new) (if you believe there is a bug within Turnstile causing this issue) or a [new discussion](https://github.com/microsoft/turnstile/discussions/new?category=q-a) (we prefer to support users in the open as a learning channel for other users.)

Let's take a closer look at the JSON object you just posted and how its properties influence the creation of the subscription:

| Property name | Description |
| --- | --- |
| `subscription_id` | We recommend using GUIDs for the subscription's unique ID. Note that you must include the same subscription ID within the URL path that you post subscription information to. |
| `subscription_name` | The name of the subscription will be displayed within both the admin and user portals. This is a user-specified friendly display name for the subscription. |
| `tenant_id` | The ID of the customer's Azure Active Directory tenant. [The tenant ID can be found within the Azure portal.](https://learn.microsoft.com/azure/active-directory/fundamentals/how-to-find-tenant) In this case, we're using your tenant ID for testing purposes. In production, this will be your customer's tenant ID. |
| `tenant_name` | The customer's name |
| `total_seats` | For user-based seating, the total number of seats that the customer has purchased; if this is a site-wide subscription (unlimited seating), set this to `null` or don't include it at all |
| `offer_id` | The name of the SaaS offering that the customer has subscribed to |
| `plan_id` | The name of the plan (e.g., bronze, silver, gold) that the customer has subscribed to |
| `state` | The initial state of the subscription; options include `active` (the subscription is ready to be used), `purchased` (the subscription has been purchased but it still being configured), `suspended` (the subscription has been temporarily suspended (e.g., for non-payment)), or `canceled` (the subscription has been canceled) |
| `is_free_trial` | Is this a free trial subscription? |
| `is_test_subscription` | Is this a test subscription? Indeed it is! |
| `limited_overflow_seating_enabled` | When set to `true`, Turnstile will provide `limited` seats when a user-based subscription has run out of seating. Turnstile makes no distinction between `limited` and `standard` seating; it is up to the SaaS app to interpret these properties and possibly limit functionality for `limited` seats. |

## 5. Finish setting up the subscription

Now that you've created your first subscription, navigate to `[user_web_app_base_url]` to finish setting it up. Normally, the customer would finish setting up their own subscription.

![Set up your subscription](images/Set%20up%20your%20subscription.png)

## 6. Get a seat in the subscription

Navigate again to `[user_web_app_base_url`]. Since the subscription has already been set up, you'll be prompted to either user or administer the subscription. From the `Use` tab, select the subscription you just finished setting up to try to obtain a seat.

![Choose a subscription](images/Choose%20a%20subscription.png)

### User redirection and obtaining seat information

You will be redirected to the SaaS app URL that you configured [in the second step](#2-run-the-turnstile-setup-script). The URL will also contain a special query string parameter (`_tt`) that, when combined with the `[base_storage_url]`, can be used to download the user's seat information for __the next five minutes__. The name of the storage account that the `[base_storage_url]` points to is obfuscated for security reasons. You'll need to URL decode the `[_tt]` parameter value before you can use it. If you're outside of the five-minute window, [you can still use Turnstile's seats API to obtain a user's seat information as demonstrated in Turnstile's end-to-end test script](https://github.com/microsoft/turnstile/blob/72412356457d1ce0645c92246b54769bbd364dcd/tests/e2e_core_api.sh#L152).

HTTP GET `[base_storage_url][decoded_tt_value]` to obtain the user's seat details. They'll look something like this:

```json
{
    "request_id": "adac2243-ecb0-4b80-859f-10e593fc4538",
    "result_code": "seat_provided",
    "seat": {
        "seat_id": "3b3bd4e9-3e6c-40bb-bc3f-d3afc902f2b2",
        "subscription_id": "c4b94771-378f-42c7-ace7-10f6245f2948",
        "occupant": {
            "user_id": "91ffea71-...",
            "user_name": "admin@admin.com",
            "tenant_id": "72f988bf-...",
            "email": "admin@admin.com"
        },
        "seating_strategy_name": "first_come_first_served",
        "seat_type": "standard",
        "reservation": null,
        "expires_utc": "2023-08-08T00:00:00Z",
        "created_utc": "2023-07-25T21:30:53.8993441Z",
        "redeemed_utc": null
    },
    "subscription": {
        "subscription_id": "c4b94771-378f-42c7-ace7-10f6245f2948",
        "subscription_name": "My first subscription!",
        "tenant_id": "72f988bf-...",
        "tenant_name": "Microsoft",
        "offer_id": "Offer 1",
        "plan_id": "Plan A",
        "state": "active",
        "admin_role_name": "",
        "user_role_name": "",
        "admin_name": "Admin",
        "admin_email": "admin@admin.com",
        "total_seats": null,
        "is_being_configured": false,
        "is_free_trial": true,
        "is_setup_complete": true,
        "is_test_subscription": true,
        "created_utc": "2023-07-25T21:17:02.5172313Z",
        "state_last_updated_utc": "2023-07-25T21:17:02.5172315Z",
        "seating_config": {
            "seating_strategy_name": "first_come_first_served",
            "low_seat_warning_level_pct": 0.25,
            "limited_overflow_seating_enabled": false,
            "seat_reservation_expiry_in_days": 14,
            "default_seat_expiry_in_days": 14
        },
        "subscriber_info": {
            "tenant_country": "US"
        },
        "source_subscription": null
    }
}
```

Let's take a look at this seat information. The `seat` section describes the seat that was created including its ID, occupant, the strategy used to create it (either `first_come_first_served` or `monthly_active_user` depending on configuration, its type (either `standard` or `limited`), and its expiration date. The `subscription` information contains detailed information about the subscription where the seat was created.



