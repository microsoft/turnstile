# SaaS Seating with Turnstile in 6 Easy Steps

Turnstile makes it easier to build SaaS apps on Azure by automating the process of providing seats (or licenses, badges, etc.) to your users. It's easy to set up (as you're about to learn firsthand) and cost-effective allowing you to scale dynamically to meet your customer's frequently changing needs. Turnstile is designed to support any Azure-based SaaS app regardless of development stack or architecture. 

## Before we get started

First, ensure that the following prerequisites are met.

* You have an active Azure Subscription. [If you don't already have one, get one free here.](https://azure.microsoft.com/free)
* You can create new Azure Active Directory app registrations. In order to create app registrations, you must be a directory administrator. For more information, see [this article](https://docs.microsoft.com/azure/active-directory/roles/permissions-reference).
* You can create resources and resource groups within the target Azure subscription. Typically, this requires at least [contributor-level access](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#contributor) to the subscription.
* You're using an Azure Active Directory work or school account. Guest or personal account won't work.

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

> __Note__: The Turnstile setup script accepts a wide range of optional parameters allowing you to customize your deployment. Learn more by running `./setup_turnstile.sh -h` from the cloud shell.

Assuming your deployment name is `dontusethis` and your region is `southcentralus` (South Central US), run the following command in the cloud shell:

```shell
./setup_turnstile.sh -n "dontusethis" -r "southcentralus"
```
This will take about 10 minutes so take a moment to freshen your coffee. ☕

Once the script is finished, it will provide you with a `Turnstile deployment summary`. Make note of these specific values because you'll need them again here in a few moments:

| Doc reference | Deployment summary label | Description |
| --- | --- | --- |
| `[deployment_name]` | `Deployment name` | Your Turnstile deployment's unique name as provided in the `-n` setup script parameter |
| `[subscription_id]` | `Deployed in Azure subscription` | The Azure subscription in which Turnstile has just been deployed |
| `[resource_group]` | `Deployment in resource group` | The Azure resource group in which Turnstile has just been deployed |
| `[api_base_url]` | `Turnstile API base URL` | The base URL of your Turnstile API |
| `[api_key]` | `Turnstile API key (secret!)` | The API key to be used when calling your Turnstile API |
| `[user_web_app_base_url]` | `User web app base URL` | The base URL of the user (customer) web app |
| `[admin_web_app_base_url]` | `Admin web app base URL` | The base URL of the admin (publisher) web app |

> __Note__: Setting the `-e` setup script flag will automatically output these values to `./[deployment_name].turnstile.env`. Handle this file with extreme care as it contains sensitive information like the Turnstile API key.

The script will also prompt you to finish setting up Turnstile by navigating to a setup page deployed within your Azure subscription. The URL will look like this:

```url
https://turn-admin-[deployment_name].azurewebsites.net/config/basics
```

Click the link and tell Turnstile a little about your company and app so it can better tailor your user's experience.

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



## 5. Get a seat

## 6. Update a subscription



