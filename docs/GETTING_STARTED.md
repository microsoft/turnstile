# SaaS Seating with Turnstile in 8 Easy Steps

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

### ⚠️ Temporary instruction (internal)

Check out the `cawatson-ui-work` branch by running the following command in the cloud shell:

```shell
git checkout cawatson-ui-work
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

Once the script is finished, it will provide you with a `Turnstile deployment summary`. Throughout the rest of this document, we're going to be referring back to these values like this:

| Deployment summary label | Document reference |
| --- | --- |
| `Deployment name` | `[deployment_name]` |
| `Deployed in Azure subscription` | `[subscription_id]` |
| `Deployed in resource group` | `[resource_group]` |
| `Azure AD tenant ID` | `[azure_ad_tenant_id]` |
| `Turnstile API base URL` | `[api_base_url]` |
| `Turnstile API key (secret!)` | `[api_key]` |
| `User web app base URL` | `[user_web_app_base_url]` |
| `Admin web app base URL` | `[admin_web_app_base_url]` |
| `Storage account base URL` | `[base_storage_url]` |

> __Note__: Setting the `-e` setup script flag will automatically output these values to `./[deployment_name].turnstile.env`. Handle this file with extreme care as it contains sensitive information like the Turnstile API key.

### Finish setting up Turnstile

The script will also prompt you to finish setting up Turnstile by navigating to a setup page deployed within your Azure subscription. The URL will look like this:

```url
https://turn-admin-[deployment_name].azurewebsites.net/config/basics
```

Click 👆 the link and tell Turnstile a little about your company and app so it can better tailor your user's experience.

![Set up Turnstile](images/Setup%20up%20Turnstile.png)

> You can return to this setup screen at any time by navigating to `[admin_web_app_base_url]/config/basics`.

## 3. Configure event integrations
Turnstile publishes a variety of subscription and seat-related events designed to make it easier to integrate with your existing apps and services. By default, Turnstile also publishes a set of template Logic Apps preconfigured to handle each event type. [The Logic Apps platform provides hundreds of prebuilt connectors so you can connect and integrate apps, data, services, and systems more easily and quickly.](https://learn.microsoft.com/azure/connectors/introduction) You can focus more on designing and implementing your solution's business logic and functionality, not on figuring out how to access your resources. These logic apps can be found in the same resource group in which Turnstile was deployed. You can find the name of this resource group in the setup script's [`Turnstile deployment summary`](#the-deployment-summary).

### Turnstile events

| Event | Logic app name | Notes
| --- | --- | -- |
| Subscription created | `turn-on-subscription-created-[deployment_name]` | 
| Subscription updated | `turn-on-subscription-updated-[deployment_name]` |
| Seat reserved | `turn-on-seat-reserved-[deployment_name]` | This event includes a link that the user can use to redeem their seat. Configure this logic app to notify the user that they've been invited to use the app. |
| Seat redeemed | `turn-on-seat-redeemed-[deployment_name]` | Occurs when a reserved seat is redeemed by its user |
| Seat provided | `turn-on-seat-provided-[deployment_name]` | Occurs when a dynamic seat (not reserved) is provided to a user |
| Reduced subscription seating available |  `turn-on-low-seat-warning-[deployment_name]` | Occurs when a user-based subscription (fixed seat count) reaches <= 25% available standard seating |
| No more seats available | `turn-on-no-seats-available-[deployment_name]` | Occurs when a user-based subscription (fixed seat count) runs out of available standard seating
| Admission granted | `turn-on-admission-granted-[deployment_name]` _(coming soon)_ | Occurs when a user is granted access to the app |
| Admission denied | `turn-on-admission-denied-[deployment_name]` _(coming soon)_ | Occurs when a user is denied access to the app. This event includes a code that indicates why the user was denied access. |

## 4. Create a subscription

Turnstile needs to know about the SaaS subscriptions that it will be providing seats for. New subscriptions are posted to Turnstile's subscriptions API endpoint. We'll create a new subscription now to understand better how the subscriptions API works.

Using your favorite API client (e.g., Postman), POST the following JSON object...

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

to this URL...

```url
[api_base_url]/api/saas/subscriptions/085a0ed4-84e0-43f7-a601-461ea81667a1?code=[api_key]
```

The API should return `200 OK`. If not, review your `[api_base_url]`, `[api_key]`, and JSON payload then try again. If you're still running into issues, please [visit our support page](../SUPPORT.md).

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
| `state` | The initial state of the subscription as described below:<br /><br /><ul><li>`active`: subscription is ready to be used</li><li>`purchased`: subscription has been purchased but is not yet ready to be used</li><li>`suspended`: subscription has been temporarily suspended (e.g., for non-payment)</li><li>`canceled`: subscription has been permanently canceled</li></ul> |
| `is_free_trial` | Is this a free trial subscription? |
| `is_test_subscription` | Is this a test subscription? Indeed it is! |
| `limited_overflow_seating_enabled` | When set to `true`, Turnstile will provide `limited` seats when a user-based subscription has run out of seating. Turnstile makes no distinction between `limited` and `standard` seating; it is up to the SaaS app to interpret seating types. Limiting 'limited' seat functionality (e.g., disabling certain features) is a common approach. |

## 5. Finish setting up the subscription

Now that you've created your first subscription, navigate to `[user_web_app_base_url]` to finish setting it up. Normally, the customer would finish setting up their own subscription.

![Set up your subscription](images/Set%20up%20your%20subscription.png)

> **Note:** You can also [use the PATCH subscription API](https://github.com/microsoft/turnstile/blob/72412356457d1ce0645c92246b54769bbd364dcd/tests/e2e_core_api.sh#L26) to finish setting up the subscription programmatically.

## 6. Get a seat in the subscription

Navigate again to `[user_web_app_base_url`] by clicking 👆 the Turnstile name (e.g., __Getting Started__ in the image below.) Since the subscription has already been set up, you'll be prompted to either **Use** or **Manage** the subscription. From the **Use** tab, select the subscription you just finished setting up to obtain a seat.

![Choose a subscription](images/Choose%20a%20subscription.png)

> **Note:** You can also use the [entry API](https://github.com/microsoft/turnstile/blob/72412356457d1ce0645c92246b54769bbd364dcd/tests/e2e_entry_api.sh#L81) to try and obtain a seat.

### User redirection and obtaining seat information

Once you're provided with a seat, you will be redirected to the SaaS app URL that you configured [earlier](#finish-setting-up-turnstile). The URL will also contain a special query string parameter (`_tt`) that, when combined with the `[base_storage_url]`, can be used to easily download the user's seat information through a regular, unauthenticated HTTP GET call for __the next five minutes__. The name of the storage account that the `[base_storage_url]` points to is obfuscated for obvious security reasons. You'll need to URL decode the `[_tt]` parameter value before you can use it. If you're outside of the five-minute window, [you can still use Turnstile's seats API to obtain a user's seat information as demonstrated in Turnstile's end-to-end test script](https://github.com/microsoft/turnstile/blob/72412356457d1ce0645c92246b54769bbd364dcd/tests/e2e_core_api.sh#L152).

`HTTP GET` `[base_storage_url][decoded_tt_value]` to obtain the user's seat details. They'll look something like this:

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

Let's take a look at this seat information. The `seat` section describes the seat that was created including its ID, occupant, the strategy used to create it (either `first_come_first_served` or `monthly_active_user` depending on configuration), its type (either `standard` or `limited`), and its expiration date. The `subscription` information contains detailed information about the subscription where the seat was created.

## 7. Manage subscriptions

Both customers and SaaS publishers can manage subscriptions through convenient web-based user interfaces.

### Manage subscriptions as a customer

Navigate to `[user_web_app_base_url]` and click 👆 on the **Manage** tab. Click 👆 **Manage My first subscription!**. You will be redirected to a screen that allows you to manage basic information about the subscription and manage and reserve seats. Note that you'll only see this screen if your email address matches the subscription's primary admin email or if you belong to the subscription admin role assuming it has been configured.

![Customer subscription management experience](images/Customer%20manage%20subscription.png)

### Manage subscriptions as a publisher

Navigate to `[admin_web_app_base_url]`. You will be redirected to a screen showing all subscriptions organized by state.

![Admin manage subscriptions](images/Admin%20manage%20subscriptions.png)

Pick a subscription then click 👆 **Manage**. You'll be redirected to a more detailed screen where you can manage the subscription (including updating the subscription state) and view occupied and reserved seats.

![Admin manage subscription](images/Admin%20manage%20subscription.png)

## 8. Reserve a seat for another user

Navigate to `[user_web_app_base_url]` and click 👆 on the **Manage** tab. Click 👆 **Manage my first subscription!** then click 👆 on the **Reserve seat** tab. You'll be presented with a screen allowing you to enter the email address of the user you wish to reserve a seat for.

> **Note:** No invitations will be sent until you update the `turn-on-seat-reserved-[deployment_name]` logic app in `[resource_group]`. We offer customers complete flexibility in how emails are sent (e.g., Azure Communication Services, SendGrid, your own SMTP server) by design.

![Reserve a seat](images/Reserve%20seat.png)

## That's it!

You're ready to start providing seats to your SaaS app using Turnstile! [Check out our support page](../SUPPORT.md) if you have any questions.


