#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

readonly TRUE="true"
readonly FALSE="false"

SECONDS=0 # Let's time it...

readonly TURNSTILE_VERSION=$(cat ../../VERSION)

# We allow the user to choose the App Service Plan SKU (see https://learn.microsoft.com/azure/app-service/overview-hosting-plans) 
# that the Turnstile API and, optionally, web app is deployed on. Unfortunately, there isn't currently (as of Feb 2023), a reliable way 
# to pull a list of available App Serice SKUs so, for now, we have a hardcoded list here. If, in the future, there is a reliable API
# call we can make to pull a list of valid SKUs, this list should instead be populated with that API response.

readonly APP_SERVICE_SKUS=(
    "D1"    # Shared
    "F1"    # Free
    "B1"    # Basic (B1 is default)
    "B2"
    "B3"
    "S1"    # Standard
    "S2"
    "S3"
    "P1"    # Premium
    "P2"
    "P3"
    "P1V2"  # Premium v2
    "P2V2"
    "P3V2"
    "I1"    # Isolated (ASE)
    "I2"
    "I3"
    "Y1"    # Consumption/Dynamic (supported only for headless deployments)
)

readonly CONSUMPTION_APP_SERVICE_SKU="Y1" # We'll be using this later...

splash() {
    echo "Turnstile | $TURNSTILE_VERSION"
    echo "Your SaaS app's friendly automated usher."
    echo "https://github.com/microsoft/turnstile"
    echo
    echo "Copyright (c) Microsoft Corporation. All rights reserved."
    echo "Licensed under the MIT License. See LICENSE in project root for more information."
    echo
}

usage() {
    echo
    echo "Usage:   $0 <-n name> <-r deployment_region> [-c publisher_config_path] [-d display_name] \\"
    echo "         [-i integration_pack] [-s app_service_plan_sku] [-H flag: headless] \\" 
    echo "         [-p flag: use_cosmos_provisioned_throughput] [-e flag: create_env_file]"
    echo 
    echo "Example: $0 -n \"dontusethis\" -r \"southcentralus\" -d \"Just an example\" \\"
    echo "         -i \"default\" -s \"P1\" -H -p -e"
    echo
    echo "Help:    $0 -h"
    echo
    echo "Parameter details"
    echo "####################################################################################################################"
    echo
    echo "<-n name>.....................................Unique name for this Turnstile deployment"
    echo "                                              Lower-case, alphanumeric, must be between 5-13 characters"
    echo "<-r deployment_region>........................The Azure region to deploy Turnstile to"
    echo "                                              For region list, run \"az account list-locations -o table\"."
    echo "[-c publisher_config_path]....................Optional; indicates the path of a custom publisher configuration"
    echo "                                              file that should be used with this deployment"
    echo "[-d display_name].............................Optional; indicates the display name of the Azure Active Directory " 
    echo "                                              app created to protect this deployment. Default display name is "
    echo "                                              \"[name]\". Admin app display name will be \"[display_name] Admin\"."
    echo "[-i integration_pack].........................Optional; indicates the name (if pack is in \"./integration_packs\")"
    echo "                                              or absolute path of integration pack to deploy. If no pack "
    echo "                                              is specified, the default pack (\"./integration_packs/default\")"
    echo "                                              will be deployed."
    echo "[-s app_service_plan_sku].....................Optional; indicates the SKU to use when creating Turnstile's" 
    echo "                                              app service plan. By default, SKU is S1 (Standard). Y1 (Consumption)"
    echo "                                              SKU can be used only with headless (-H) deployments." 
    echo "[-H flag: headless]...........................Optional; if flag is set, only the Turnstile API will be deployed."
    echo "                                              If not set, the API and both user and admin web apps will be deployed."
    echo "[-p flag: use_cosmos_provisioned_throughput]..Optional; if flag is set, Turnstile's Cosmos account will"
    echo "                                              be created in provisioned throughput mode instead of the default"
    echo "                                              serverless mode."
    echo "[-e flag: create_env_file]....................Optional; if flag is set, a file (\"./[name].turnstile.env\") "
    echo "                                              will be generated that contains all the properties from the deployment "
    echo "                                              summary as environment variables. NOTE that this file contains secrets. "
    echo "                                              Handle with extreme care and delete when no longer needed."
    echo "[-h flag: help]...............................Just presents this usage information"
    echo
}

check_az() {
    az version >/dev/null

    if [[ $? -ne 0 ]]; then
        echo "❌   Please install the Azure CLI before continuing. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
        return 1
    else
        echo "✔   Azure CLI installed."
    fi
}

check_zip() {
    zip -h >/dev/null

    if [[ $? -ne 0 ]]; then
        echo "❌   Please install zip before continuing."
        return 1
    else
        echo "✔   zip installed."
    fi
}

check_dotnet() {
    dotnet_version=$(dotnet --version)

    if [[ $dotnet_version == 8.*.* ]]; then # Needs to be .NET 8
        echo "✔   .NET [$dotnet_version] installed."
    else
        read -p "⚠️  .NET 8 is required to run this script but is not installed. Would you like to install it now? [Y/n]" install_dotnet

        case "$install_dotnet" in
            [yY1]   )
                wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
                chmod +x ./dotnet-install.sh
                ./dotnet-install.sh 

                if [[ $? == 0 ]]; then
                    export PATH="$HOME/.dotnet:$PATH"
                    dotnet_version=$(dotnet --version)
                    echo "✔   .NET [$dotnet_version] installed."
                    return 0
                else
                    echo "❌   .NET 8 installation failed. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
                    return 1
                fi
            ;;
            *       )
                echo "❌   Please install .NET 8 before continuing. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
                return 1
            ;;
        esac
    fi
}

check_app_service_sku() {
    sku=$1
    headless=$2

    if [[ "${APP_SERVICE_SKUS[*]}" =~ "${sku}" ]]; then
        if [[ "$headless" == "$FALSE" && "$sku" == "$CONSUMPTION_APP_SERVICE_SKU" ]]; then # Only functions (the API layer) can be deployed to a consumption plan.
            echo "❌   [$sku] Azure App Service plan SKU is only valid when used with headless/API-only (-H) deployments."
            return 1
        else
            echo "✔   [$sku] is a valid Azure App Service plan SKU for this deployment."
        fi
    else
        echo "❌   [$sku] is not a Azure App Service plan SKU, but these are..."
        echo
        printf '%s\n' "${APP_SERVICE_SKUS[@]}"
        echo
        echo "For more information, see [ https://learn.microsoft.com/azure/app-service/overview-hosting-plans ]."
        return 1
    fi
}

check_deployment_region() {
    region=$1

    if [[ -z $region ]]; then
         echo "❌   Deployment region <-r> is required. Please choose a region and try again..."
            echo
            az account list-locations --output table --query "[].name"
            echo
            return 1
    else
        region_display_name=$(az account list-locations -o tsv --query "[?name=='$region'].displayName")

        if [[ -z $region_display_name ]]; then
            echo "❌   [$region] is not a valid Azure region, but these are..."
            echo
            az account list-locations --output table --query "[].name"
            echo
            return 1
        else
            echo "✔   [$region] is a valid Azure region ($region_display_name)."
        fi
    fi
}

check_deployment_name() {
    name=$1

    if [[ $name =~ ^[a-z0-9]{5,13}$ ]]; then
        echo "✔   [$name] is a valid Turnstile deployment name."
        return 0
    elif [[ -z $name ]]; then
        echo "❌   Turnstile deployment name <-n> is required. The name must contain only lowercase letters and numbers and be between 5 and 13 characters in length."
        return 1
    else
        echo "❌   [$name] is not a valid Turnstile deployment name. The name must contain only lowercase letters and numbers and be between 5 and 13 characters in length."
        return 1
    fi
}

# Howdy!

splash

# Get our parameters...

p_headless="$FALSE" # Full deployment by default
p_app_service_sku="B1" # App service plan Basic 1 (B1) by default
p_integration_pack="default" # Default integration pack by default
p_use_cosmos_provisioned_throughput="$FALSE" # Cosmos serverless by default

while getopts "s:c:d:n:r:i:hHpe" opt; do
    case $opt in
        s)
            p_app_service_sku=$(echo "$OPTARG" | tr '[:lower:]' '[:upper:]') # Always uppercase for consistency...
        ;;
        c)
            p_publisher_config_path=$OPTARG
        ;;
        d)
            p_display_name=$OPTARG
        ;;
        i)
            p_integration_pack=$OPTARG
        ;;
        n)
            p_deployment_name=$OPTARG
        ;;
        r)
            p_deployment_region=$OPTARG
        ;;
        H)
            p_headless="$TRUE"

            # This flag allows you to deploy Turnstile in "headless" mode. Headless mode deploys _only_
            # the API and supporting resources. It does not deploy the web app and, consequently, doesn't
            # do any AAD configuration.
        ;;
        p)
            p_use_cosmos_provisioned_throughput="$TRUE"
        ;;
        e)
            p_create_env_file="$TRUE"
        ;;
        h)
            usage
            exit 0 # Help was expected
        ;;
        \?)
            usage
            exit 1 # Help wasn't expected
        ;;
    esac
done

echo "Validating script parameters..."

[[ -z p_deployment_name || -z p_deployment_region ]] && { usage; exit 1; }

check_app_service_sku   "$p_app_service_sku" "$p_headless";     [[ $? -ne 0 ]] && param_check_failed=1
check_deployment_region "$p_deployment_region";                 [[ $? -ne 0 ]] && param_check_failed=1
check_deployment_name   "$p_deployment_name";                   [[ $? -ne 0 ]] && param_check_failed=1

if [[ -z $param_check_failed ]]; then
    echo "✔   All setup parameters are valid."
else
    echo "❌   Parameter validation failed. Please review and try again."
    echo
    usage
    exit 1
fi

# Make sure all pre-reqs are installed...

echo "Checking setup prerequisites..."

check_az;           [[ $? -ne 0 ]] && prereq_check_failed=1
check_dotnet;       [[ $? -ne 0 ]] && prereq_check_failed=1
check_zip;          [[ $? -ne 0 ]] && prereq_check_failed=1

if [[ -z $prereq_check_failed ]]; then
    echo "✔   All setup prerequisites installed."
else
    echo "❌   Please install all setup prerequisites then try again."
    exit 1
fi

# Try to upgrade Bicep...

az bicep upgrade

# Log in the user if they aren't already...

while [[ -z $current_user_oid ]]; do
    current_user_oid=$(az ad signed-in-user show --query id --output tsv 2>/dev/null);
    if [[ -z $current_user_oid ]]; then az login; fi;
done

if [[ "$p_headless" == "$TRUE" ]]; then 
    echo "ℹ️   [-H]: This is a headless deployment. Only the API layer will be deployed."
else
    echo "ℹ️   This is a FULL deployment. Both the API and web app layers will be deployed."
fi

p_deployment_name=$(echo "$p_deployment_name" | tr '[:upper:]' '[:lower:]') # Lower the deployment name...

subscription_id=$(az account show --query id --output tsv);
current_user_tid=$(az account show --query tenantId --output tsv);

if [[ -z $p_display_name ]]; then
    display_name="Turnstile $p_deployment_name"
else
    display_name="$p_display_name"
fi

entra_api_app_name="$display_name API"

echo "🛡️   Creating Turnstile API app registration [$entra_api_app_name] in Entra tenant [$current_user_tid]..."

create_api_app_response=$(az ad app create \
    --display-name "$entra_api_app_name" \
    --enable-access-token-issuance true \
    --sign-in-audience 'AzureADMyOrg')

entra_api_object_id=$(echo "$create_api_app_response" | jq -r ".id")
entra_api_app_id=$(echo "$create_api_app_response" | jq -r ".appId")

# Expose the app registration as an API...

az ad app update \
    --id "$entra_api_object_id" \
    --identifier-uris "api://$entra_api_app_id"

if [[ "$p_headless" == "$FALSE" ]]; then

    # Create the app registrations in Entra...

    entra_user_app_name="$display_name Users"
    entra_admin_app_name="$display_name Admin"

    echo "🛡️   Creating Turnstile user app registration [$entra_user_app_name] in Entra tenant [$current_user_tid]..."

    create_user_app_response=$(az ad app create \
        --display-name "$entra_user_app_name" \
        --enable-id-token-issuance true \
        --sign-in-audience "AzureADandPersonalMicrosoftAccount" \
        --web-redirect-uris "https://turn-web-${p_deployment_name}.azurewebsites.net/signin-oidc")

    entra_user_app_object_id=$(echo "$create_user_app_response" | jq -r ".id")
    entra_user_app_id=$(echo "$create_user_app_response" | jq -r ".appId")
    entra_user_app_domain=$(echo "$create_user_app_response" | jq -r ".publisherDomain")

    echo "🛡️   Creating Turnstile admin app registration [$entra_admin_app_name] in Entra tenant [$current_user_tid]..."

    create_admin_app_response=$(az ad app create \
        --display-name "$entra_admin_app_name" \
        --enable-id-token-issuance true \
        --sign-in-audience "AzureADMyOrg" \
        --web-redirect-uris "https://turn-admin-${p_deployment_name}.azurewebsites.net/signin-oidc")

    entra_admin_app_id=$(echo "$create_admin_app_response" | jq -r ".appId")
    entra_admin_app_object_id=$(echo "$create_admin_app_response" | jq -r ".id")
    entra_admin_app_domain=$(echo "$create_admin_app_response" | jq -r ".publisherDomain")

fi

# Create our resource group if it doesn't already exist...

resource_group_name="turnstile-$p_deployment_name"

if [[ $(az group exists --resource-group "$resource_group_name" --output tsv) == false ]]; then
    echo "Creating resource group [$resource_group_name]..."

    az group create \
        --location "$p_deployment_region" \
        --name "$resource_group_name" \
        --tags \
            "Turnstile Name"="$p_deployment_name" \
            "Turnstile Version"="$TURNSTILE_VERSION"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
fi

# Make sure Bicep is up-to-date...

az bicep upgrade

az_deployment_name="turnstile-deploy-$p_deployment_name"
deployment_url="https://portal.azure.com/#view/HubsExtension/DeploymentDetailsBlade/~/overview/id/%2Fsubscriptions%2F$subscription_id%2FresourceGroups%2F$resource_group_name%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2F$az_deployment_name"

echo "🦾   Deploying core Turnstile Bicep template [$az_deployment_name] to subscription [$subscription_id] resource group [$resource_group_name]... "
echo
echo "⏳   This may take a while. Monitor the deployment live at:"
echo "    [ $deployment_url ]."
echo

az deployment group create \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --template-file "./turnstile_deploy.bicep" \
    --parameters \
        appServicePlanSku="$p_app_service_sku" \
        deploymentName="$p_deployment_name" \
        aadTenantId="$current_user_tid" \
        apiAadClientId="$entra_api_app_id" \
        userWebAppAadClientId="$entra_user_app_id" \
        adminWebAppAadClientId="$entra_admin_app_id" \
        adminWebAppAadDomain="$entra_admin_app_domain" \
        useCosmosProvisionedThroughput="$p_use_cosmos_provisioned_throughput" \
        turnstileVersion="$TURNSTILE_VERSION" \
        headless="$p_headless" 2>/dev/null

echo

if [[ $? == 0 ]]; then
    echo "✔   Core Turnstile Bicep template successfully deployed.";
else
    # If this template deployment failed, we really are done at this point.
    # We can't do anything more without confirmation that these resources are there.

    echo "❌  Core Turnstile Bicep template deployment failed. See [ $deployment_url ] for more information. Setup failed."
    exit 1
fi

if [[ "$p_headless" == "$FALSE" ]]; then

    web_app_name=$(az deployment group show \
        --resource-group "$resource_group_name" \
        --name "$az_deployment_name" \
        --query properties.outputs.userWebAppName.value \
        --output tsv);

    web_app_base_url=$(az deployment group show \
        --resource-group "$resource_group_name" \
        --name "$az_deployment_name" \
        --query properties.outputs.userWebAppBaseUrl.value \
        --output tsv);

    admin_web_app_name=$(az deployment group show \
        --resource-group "$resource_group_name" \
        --name "$az_deployment_name" \
        --query properties.outputs.adminWebAppName.value \
        --output tsv)

    admin_web_app_base_url=$(az deployment group show \
        --resource-group "$resource_group_name" \
        --name "$az_deployment_name" \
        --query properties.outputs.adminWebAppBaseUrl.value \
        --output tsv)

fi

api_app_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.apiAppName.value \
    --output tsv);

storage_acct_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.storageAccountName.value \
    --output tsv);

storage_acct_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.storageAccountId.value \
    --output tsv);

integration_mid_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.managedIdId.value \
    --output tsv);

integration_mid_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.managedIdName.value \
    --output tsv);

event_grid_connection_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.eventGridConnectionId.value \
    --output tsv);

event_grid_connection_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.eventGridConnectionName.value \
    --output tsv);

event_grid_topic_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.eventGridTopicId.value \
    --output tsv);

deployment_type=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.deploymentType.value \
    --output tsv)

deployment_profile=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.deploymentProfile.value \
    --output tsv)

cosmos_acct_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.cosmosAccountName.value \
    --output tsv)

cosmos_acct_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.cosmosAccountId.value \
    --output tsv)

echo "⚙️   Saving [$deployment_type] deployment profile..."

# I'm going somewhere cool with this. Stay tuned.

az storage blob upload \
    --account-name "$storage_acct_name" \
    --container-name "turn-configuration" \
    --data "$deployment_profile" \
    --name "deployment/$deployment_type/profile.json"

echo "🔐   Configuring integration identity [$integration_mid_name] access..."

# First, we'll assign roles to the integration identity...

integration_mid_sp_id=$(az identity show \
    --ids "$integration_mid_id" \
    --query principalId \
    --output tsv)

# Make sure that the integration identity can create event grid topic subscriptions...

az role assignment create \
    --assignee-object-id "$integration_mid_sp_id" \
    --assignee-principal-type "ServicePrincipal" \
    --role "Contributor" \
    --scope "$event_grid_topic_id" \
    --description "Turnstile integration identity can create event grid topic subscriptions."

# Make sure that the integration identity can publish/subscribe to event grid...

az role assignment create \
    --assignee-object-id "$integration_mid_sp_id" \
    --assignee-principal-type "ServicePrincipal" \
    --role "EventGrid Data Contributor" \
    --scope "$event_grid_topic_id" \
    --description "Turnstile integration identity can publish/subscribe to event grid."

# Next, we'll assign roles to the API identity...

api_mid_name=$(az functionapp identity show \
    --name "$api_app_name" \
    --subscription "$subscription_id" \
    --resource-group "$resource_group_name" \
    --query name \
    --output tsv)

api_mid_id=$(az functionapp identity show \
    --name "$api_app_name" \
    --subscription "$subscription_id" \
    --resource-group "$resource_group_name" \
    --query principalId \
    --output tsv)

echo "🔐   Configuring API identity access..."

# Make sure that the API identity can create storage containers (if they don't already exist)...

az role assignment create \
    --assignee-object-id "$api_mid_id" \
    --assignee-principal-type "ServicePrincipal" \
    --role "Contributor" \
    --scope "$storage_acct_id" \
    --description "Turnstile API identity can create storage containers."

# Make sure that the API identity can read/write blobs in the storage account...

az role assignment create \
    --assignee-object-id "$api_mid_id" \
    --assignee-principal-type "ServicePrincipal" \
    --role "Storage Blob Data Owner" \
    --scope "$storage_acct_id" \
    --description "Turnstile API identity can read/write blobs in the storage account."

# Make sure that the API identity can create event grid subscriptions...

az role assignment create \
    --assignee-object-id "$api_mid_id" \
    --assignee-principal-type "ServicePrincipal" \
    --role "Contributor" \
    --scope "$event_grid_topic_id" \
    --description "Turnstile API identity can create event grid subscriptions."

# Make sure that the API identity can publish/subscribe to event grid...

az role assignment create \
    --assignee-object-id "$api_mid_id" \
    --assignee-principal-type "ServicePrincipal" \
    --role "EventGrid Data Contributor" \
    --scope "$event_grid_topic_id" \
    --description "Turnstile API identity can publish/subscribe to event grid."

# Make sure that the API identity can read/write to the Cosmos DB...

az cosmosdb sql role assignment create \
    --account-name "$cosmos_acct_name" \
    --resource-group "$resource_group_name" \
    --role-definition-name "Cosmos DB Built-in Data Contributor" \
    --scope "/dbs/turnstiledb" \
    --principal-id "$api_mid_id"

# Deploy integration pack.

# When a user provides the integration pack parameter (-i), we check two places for it.
# First, we check to see if the user provided an absolute path, likely outside of this repo,
# to an integration pack. If we can't find the integration pack at the absolute path, we then
# check our local integration_packs folder (./integration_packs) for one with the same name
# provided. If there's a deploy_pack.bicep at either path, this script tries to run it. If we can't
# resolve an integration pack, we don't deploy any at all (not even default) because the assumption
# is that something was entered wrong and we don't want the user to have to go back and clean up
# the default integration pack if that isn't what they intended to deploy. Design inspired by the
# way that node_modules work.

# Originally, we didn't deploy integration packs in headless mode but, since we plan on introducing
# a "no op" integration pack that doesn't deploy any event handlers, we'll leave it in. There is no escaping integration packs!

integration_pack="${p_integration_pack#/}" # Trim leading...
integration_pack="${p_integration_pack%/}" # and trailing slashes.

pack_absolute_path="$p_integration_pack/deploy_pack.bicep" # Absolute...
pack_relative_path="./integration_packs/$p_integration_pack/deploy_pack.bicep" # and relative pack paths.

if [[ -f "$pack_absolute_path" ]]; then # Check the absolute path first...
    pack_path="$pack_absolute_path"
elif [[ -f "$pack_relative_path" ]]; then # then check the relative path.
    pack_path="$pack_relative_path"
fi

if [[ -z "$pack_path" ]]; then
    echo "⚠️   Integration pack [$p_integration_pack] not found at [$pack_absolute_path] or [$pack_relative_path]. No integration pack will be deployed."
else
    pack_deployment_name="turn-pack-deploy-$p_deployment_name"
    pack_deployment_url="https://portal.azure.com/#view/HubsExtension/DeploymentDetailsBlade/~/overview/id/%2Fsubscriptions%2F$subscription_id%2FresourceGroups%2F$resource_group_name%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2F$pack_deployment_name"

    echo "🦾   Deploying [$p_integration_pack ($pack_path)] integration pack..."
    echo
    echo "⏳   This may take a while. Monitor the deployment live at:"
    echo "    [ $pack_deployment_url ]."
    echo

    az deployment group create \
        --resource-group "$resource_group_name" \
        --name "turn-pack-deploy-$p_deployment_name" \
        --template-file "$pack_path" \
        --parameters \
            deploymentName="$p_deployment_name" \
            managedIdId="$integration_mid_id" \
            eventGridConnectionId="$event_grid_connection_id" \
            eventGridConnectionName="$event_grid_connection_name" \
            eventGridTopicId="$event_grid_topic_id" 2>/dev/null

    [[ $? -eq 0 ]] && echo "✔   Integration pack [$p_integration_pack ($pack_path)] deployed.";
    [[ $? -ne 0 ]] && echo "⚠️   Integration pack [$p_integration_pack ($pack_path)] deployment failed. See [ $pack_deployment_url ] for more information.";
fi

echo "⚙️   Applying initial publisher configuration..."

if [[ -z $p_publisher_config_path ]]; then
    publisher_config_path="default_publisher_config.json"
else # The user can provide a custom publisher configuration file via the -c switch...
    publisher_config_path=$p_publisher_config_path
fi

az storage blob upload \
    --account-name "$storage_acct_name" \
    --container-name "turn-configuration" \
    --file "$publisher_config_path" \
    --name "publisher_config.json"

# Build and prepare the API and function apps for deployment to the cloud...

echo "🛠️   Building Turnstile app(s)..."

dotnet publish -c Release -o ./api_topublish ../Turnstile.Api/Turnstile.Api.csproj

if [[ "$p_headless" == "$FALSE" ]]; then
    dotnet publish -c Release -o ./web_topublish ../Turnstile.Web/Turnstile.Web.csproj
    dotnet publish -c Release -o ./admin_web_topublish ../Turnstile.Web.Admin/Turnstile.Web.Admin.csproj
fi

# Once the builds are finished, pack them up for deployment.

cd ./api_topublish
zip -r ../api_topublish.zip . >/dev/null
cd ..

if [[ "$p_headless" == "$FALSE" ]]; then

    cd ./web_topublish
    zip -r ../web_topublish.zip . >/dev/null
    cd ..

    cd ./admin_web_topublish
    zip -r ../admin_web_topublish.zip . >/dev/null
    cd ..

fi

echo "☁️    Publishing Turnstile app(s)..."

# We can deploy both apps in parallel and hopefully save a little time...

az functionapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --src "./api_topublish.zip" &

deploy_api_pid=$!

if [[ "$p_headless" == "$FALSE" ]]; then

    az webapp deployment source config-zip \
        --resource-group "$resource_group_name" \
        --name "$web_app_name" \
        --src "./web_topublish.zip" &

    deploy_web_pid=$!

    az webapp deployment source config-zip \
        --resource-group "$resource_group_name" \
        --name "$admin_web_app_name" \
        --src "./admin_web_topublish.zip" &

    deploy_admin_web_pid=$!

    wait $deploy_web_pid
    wait $deploy_admin_web_pid

fi

wait $deploy_api_pid

echo "🧹   Cleaning up..."

rm -rf ./api_topublish >/dev/null
rm -rf ./api_topublish.zip >/dev/null

if [[ "$p_headless" == "$FALSE" ]]; then

    rm -rf ./web_topublish >/dev/null
    rm -rf ./web_topublish.zip >/dev/null
    rm -rf ./admin_web_topublish >/dev/null
    rm -rf ./admin_web_topublish.zip >/dev/null

fi

echo "⭐   Turnstile deployment complete. It took [$SECONDS] seconds."
echo

api_base_url="https://$api_app_name.azurewebsites.net"

if [[ -n $p_create_env_file ]]; then
    env_file_path="./$p_deployment_name.turnstile.env"

    echo "📄   [-e]: Writing deployment environment variables to [$env_file_path]..."
    echo

    # Alright, let's write this env file...
    # ${var//\'/\\\'} escapes single quotes.

    echo "# Turnstile deployment [$p_deployment_name] environment variables"                >> $env_file_path
    echo                                                                                    >> $env_file_path
    echo "# WARNING: File contains secrets. Treat with extreme caution."                    >> $env_file_path
    echo "#          Delete when no longer needed."                                         >> $env_file_path
    echo                                                                                    >> $env_file_path
    echo "TURNSTILE_DEPLOYMENT_NAME='${p_deployment_name//\'/\\\'}'"                        >> $env_file_path
    echo "TURNSTILE_DEPLOYMENT_VERSION='${TURNSTILE_VERSION//\'/\\\'}'"                     >> $env_file_path
    echo "TURNSTILE_AZURE_SUBSCRIPTION_ID='${subscription_id//\'/\\\'}'"                    >> $env_file_path
    echo "TURNSTILE_AZURE_RESOURCE_GROUP_NAME='${resource_group_name//\'/\\\'}'"            >> $env_file_path
    echo "TURNSTILE_AZURE_AD_TENANT_ID='${current_user_tid//\'/\\\'}'"                      >> $env_file_path
    echo "TURNSTILE_API_BASE_URL='${api_base_url//\'/\\\'}'"                                >> $env_file_path
    echo "TURNSTILE_API_ENTRA_APP_ID='${entra_api_app_id//\'/\\\'}'"                        >> $env_file_path
fi

echo "ℹ️   Turnstile deployment summary"
echo
echo "Deployment name...................[$p_deployment_name]"
echo "Deployment version................[$TURNSTILE_VERSION]"
echo "Deployed in Azure subscription....[$subscription_id]"
echo "Deployed in resource group........[$resource_group_name]"
echo "Azure AD tenant ID................[$current_user_tid]"
echo "API base URL......................[$api_base_url]"
echo "API Entra app ID..................[$entra_api_app_id]"

if [[ "$p_headless" == "$FALSE" ]]; then
    web_app_base_url="$web_app_base_url/"
    admin_web_app_base_url="$admin_web_app_base_url"
    storage_account_base_url="https://$storage_account_name.blob.core.windows.net"

    if [[ -n $p_create_env_file ]]; then
        echo "TURNSTILE_USER_WEB_APP_BASE_URL='${web_app_base_url//\'/\\\'}'"               >> $env_file_path
        echo "TURNSTILE_ADMIN_WEB_APP_BASE_URL='${admin_web_app_base_url//\'/\\\'}'"        >> $env_file_path
        echo "TURNSTILE_USER_WEB_APP_ENTRA_APP_ID='${entra_user_app_id//\'/\\\'}'"          >> $env_file_path
        echo "TURNSTILE_ADMIN_WEB_APP_ENTRA_APP_ID='${entra_admin_app_id//\'/\\\'}'"        >> $env_file_path
        echo "TURNSTILE_STORAGE_ACCOUNT_BASE_URL='${storage_account_base_url//\'/\\\'}'"    >> $env_file_path
    fi

    echo "User web app base URL.............[$web_app_base_url]"
    echo "Admin web app base URL............[$admin_web_app_base_url]"
    echo "User web app Entra app ID.........[$entra_user_app_id]"
    echo "Admin web app Entra app ID........[$entra_admin_app_id]"
    echo "Storage account base URL..........[$storage_account_base_url]"
    echo
    echo "➡️   Please go to [ $admin_web_app_base_url/config/basics ] to complete setup."
fi

echo

if [[ -n $p_create_env_file ]]; then
    echo "📄   [-e]: Deployment environment variables written to [$env_file_path]."
    echo "⚠️   [-e]: WARNING: [$env_file_path] contains secrets. Treat this file with extreme caution and delete when done."
fi
