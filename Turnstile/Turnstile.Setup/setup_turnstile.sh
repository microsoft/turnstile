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
    "B1"    # Basic
    "B2"
    "B3"
    "S1"    # Standard (S1 is default)
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

usage() {
    echo
    echo "Usage:   $0 <-n name> <-r deployment_region> [-c publisher_config_path] [-d display_name] \\"
    echo "         [-i integration_pack] [-s app_service_plan_sku] [-H flag: headless] \\" 
    echo "         [-p flag: use_cosmos_provisioned_throughput]"
    echo 
    echo "Example: $0 -n \"dontusethis\" -r \"southcentralus\" -d \"Just an example\""
    echo "         -i \"default\" -s \"P1\" -H -p"
    echo
    echo "Parameter details"
    echo "--------------------------------------------------------------------------------------------------------------------"
    echo
    echo "<-n name>.....................................Unique name for this Turnstile deployment"
    echo "                                              Lower-case, alphanumeric, must be between 5-13 characters"
    echo "<-r deployment_region>........................The Azure region to deploy Turnstile to"
    echo "                                              For region list, run \"az account list-locations -o table\"."
    echo "[-c publisher_config_path]....................Optional; indicates the path of a custom publisher configuration"
    echo "                                              file that should be used with this deployment"
    echo "[-d display_name].............................Optional; indicates the display name of the Azure Active "
    echo "                                              Directory app created to protect this deployment. Admin "
    echo "                                              app display name will be \"[display_name] Admin\"."
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

    if [[ $dotnet_version == 6.* ]]; then # Needs to be .NET 6
        echo "✔   .NET [$dotnet_version] installed."
    else
        read -p "⚠️  .NET 6 is required to run this script but is not installed. Would you like to install it now? [Y/n]" install_dotnet

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
                    echo "❌   .NET 6 installation failed. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
                    return 1
                fi
            ;;
            *       )
                echo "❌   Please install .NET 6 before continuing. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
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

splash() {
    echo "Turnstile | $TURNSTILE_VERSION"
    echo "Your SaaS app's friendly automated usher."
    echo "https://github.com/microsoft/turnstile"
    echo
    echo "Copyright (c) Microsoft Corporation. All rights reserved."
    echo "Licensed under the MIT License. See LICENSE in project root for more information."
    echo
}

# Howdy!

splash

# Get our parameters...

p_headless="$FALSE"
p_app_service_sku="S1"
p_integration_pack="default"
p_use_cosmos_provisioned_throughput="$FALSE"

while getopts "s:c:d:n:r:i:Hp" opt; do
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
        \?)
            usage
            exit 1
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
# check_dotnet;       [[ $? -ne 0 ]] && prereq_check_failed=1
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
    echo "ℹ️   This is a HEADLESS (-h) deployment. Only the Turnstile API layer will be deployed."
else
    echo "ℹ️   This is a FULL deployment. Both the Turnstile API and web app layers will be deployed."
fi

p_deployment_name=$(echo "$p_deployment_name" | tr '[:upper:]' '[:lower:]') # Lower the deployment name...

if [[ -z $p_display_name ]]; then
    display_name="Turnstile $p_deployment_name"
else
    display_name="$p_display_name"
fi

if [[ "$p_headless" == "$FALSE" ]]; then

    # We're creating the app registration used to authenticate users to the web app here which is different
    # than the managed identity we create later for resource access.

    # Create the app registration in AAD...

    aad_app_name="$display_name"
    admin_aad_app_name="$display_name Admin"

    echo "🛡️   Creating Azure Active Directory app registration [$aad_app_name] for Turnstile user web app..."

    graph_token=$(az account get-access-token \
        --resource-type ms-graph \
        --query accessToken \
        --output tsv);

    tenant_admin_role_id=$(cat /proc/sys/kernel/random/uuid)
    turnstile_admin_role_id=$(cat /proc/sys/kernel/random/uuid)
    create_app_json=$(cat ./aad/manifest.json)
    create_app_json="${create_app_json/__aad_app_name__/${aad_app_name}}"
    create_app_json="${create_app_json/__deployment_name__/${p_deployment_name}}"
    create_app_json="${create_app_json/__tenant_admin_role_id__/${tenant_admin_role_id}}"
    create_app_json="${create_app_json/__turnstile_admin_role_id__/${turnstile_admin_role_id}}"

    # We occasionally run into some consistency issues with these Azure AD calls
    # so we wrap each one in a transient fault handling block. This is an implementation of
    # the retry pattern which you can learn about at https://docs.microsoft.com/en-us/azure/architecture/patterns/retry.

    for i1 in {1..5}; do
        create_app_response=$(curl \
            -X POST \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $graph_token" \
            -d "$create_app_json" \
            "https://graph.microsoft.com/v1.0/applications")

        # Thanks, @cherchyk, for teaching me how to use jq! 👋🏻

        aad_object_id=$(echo "$create_app_response" | jq -r ".id")
        aad_app_id=$(echo "$create_app_response" | jq -r ".appId")

        if [[ -z $aad_object_id || -z $aad_app_id || $aad_object_id == null || $aad_app_id == null ]]; then
            if [[ $i1 == 5 ]]; then
                # We tried five times and it's still not working. Time to give up, unfortunately.
                echo "❌   Failed to create Azure Active Directory app registration [$aad_app_name] for Turnstile user web app. Setup failed."
                exit 1
            else
                sleep_for=$((2**i1)) # Exponential backoff - 2..4..8..16..32 second wait between retries
                echo "⚠️   Trying to create Azure Active Directory app registration [$aad_app_name] for Turnstile user web app again in [$sleep_for] seconds..."
                sleep $sleep_for
            fi
        else
            break
        fi
    done

    # Regardless of whether or not the user provided an existing AAD app ID, we still need to create
    # a client secret that the web app can use for authentication.

    echo "🛡️   Creating Azure Active Directory client credentials for Turnstile user web app [$aad_app_name]..."

    add_password_json=$(cat ./aad/add_password.json)

    for i2 in {1..5}; do
        add_password_response=$(curl \
            -X POST \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $graph_token" \
            -d "$add_password_json" \
            "https://graph.microsoft.com/v1.0/applications/$aad_object_id/addPassword")

        aad_app_secret=$(echo "$add_password_response" | jq -r ".secretText")

        if [[ -z $aad_app_secret || $aad_app_secret == null ]]; then
            if [[ $i2 == 5 ]]; then
                echo "❌   Failed to create Azure Active Directory client credentials for Turnstile user web app [$aad_app_name]. Setup failed."
                exit 1
            else
                sleep_for=$((2**i2))
                echo "⚠️   Trying to create Azure Active Directory client credentials for Turnstile user web app [$aad_app_name] again in [$sleep_for] seconds..."
                sleep $sleep_for
            fi
        else
            break
        fi
    done

    echo "🛡️   Creating Azure Active Directory service principal for Turnstile user web app [$aad_app_name]..."

    for i3 in {1..5}; do
        aad_sp_id=$(az ad sp create --id "$aad_app_id" --query id --output tsv)

        if [[ -z $aad_sp_id || $aad_sp_id == null ]]; then
            if [[ $i3 == 5 ]]; then
                echo "❌   Failed to create Azure Active Directory service principal for Turnstile user web app [$aad_app_name]. Setup failed."
                exit 1
            else
                sleep_for=$((2**i3))
                echo "⚠️   Trying to create Azure Active Directory service principal for Turnstile user web app [$aad_app_name] in [$sleep_for] seconds."
                sleep $sleep_for
            fi     
        else
            break
        fi
    done

    echo "🛡️   Creating Azure Active Directory app registration [$admin_aad_app_name] for Turnstile admin web app..."

    for i4 in {1..5}; do
        create_admin_app_response=$(az ad app create \
            --display-name "$admin_aad_app_name" \
            --enable-id-token-issuance true \
            --sign-in-audience "AzureADMyOrg" \
            --web-redirect-uris "https://turn-admin-${p_deployment_name}.azurewebsites.net/signin-oidc" \
            --output json)

        admin_aad_app_id=$(echo "$create_admin_app_response" | jq -r ".appId")
        admin_aad_object_id=$(echo "$create_admin_app_response" | jq -r ".id")
        admin_aad_domain=$(echo "$create_admin_app_response" | jq -r ".publisherDomain")

        if [[ -z $admin_aad_app_id || $admin_aad_app_id == null || -z $admin_aad_object_id || $admin_aad_object_id == null || -z $admin_aad_domain || $admin_aad_domain == null ]]; then
            if [[ $i4 == 5 ]]; then
                echo "❌   Failed to create Azure Active Directory app registration [$admin_aad_app_name] for Turnstile admin web app. Setup failed."
                exit 1
            else
                sleep_for=$((2**i4))
                echo "⚠️   Trying to create Azure Active Directory app registration [$admin_aad_app_name] for Turnstile admin web app again in [$sleep_for] seconds."
                sleep $sleep_for
            fi     
        else
            break
        fi
    done

    echo "🛡️   Creating Azure Active Directory client credentials for Turnstile admin web app [$admin_aad_app_name]..."

    for i5 in {1..5}; do
        create_admin_creds_response=$(az ad app credential reset \
            --id "$admin_aad_app_id" \
            --append \
            --output json \
            --years 100)

        admin_aad_app_secret=$(echo "$create_admin_creds_response" | jq -r ".password")

        if [[ -z $admin_aad_app_secret || $admin_aad_app_secret == null ]]; then
            if [[ $i5 == 5 ]]; then
                echo "❌   Failed to create Azure Active Directory client credentials for Turnstile admin web app [$admin_web_app_name]. Setup failed."
                exit 1
            else
                sleep_for=$((2**i5))
                echo "⚠️   Trying to create Azure Active Directory client credentials for Turnstile admin web app [$admin_web_app_name] in [$sleep_for] seconds..."
                sleep $sleep_for
            fi
        else
            break
        fi
    done
fi

# Where are we?

subscription_id=$(az account show --query id --output tsv);
current_user_tid=$(az account show --query tenantId --output tsv);

# Create our resource group if it doesn't already exist...

resource_group_name="turnstile-$p_deployment_name"

if [[ $(az group exists --resource-group "$resource_group_name" --output tsv) == false ]]; then
    echo "Creating resource group [$resource_group_name]..."

    az group create \
        --location "$p_deployment_region" \
        --name "$resource_group_name" \
        --tags \
            "Turnstile Deployment Name"="$p_deployment_name" \
            "Turnstile Version"="$TURNSTILE_VERSION"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
fi

az bicep upgrade

az_deployment_name="turnstile-deploy-$p_deployment_name"

echo "🦾   Deploying core Turnstile Bicep template [$az_deployment_name] to subscription [$subscription_id] resource group [$resource_group_name]. This may take a while..."

az deployment group create \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --template-file "./turnstile_deploy.bicep" \
    --parameters \
        appServicePlanSku="$p_app_service_sku" \
        deploymentName="$p_deployment_name" \
        aadTenantId="$current_user_tid" \
        userWebAppAadClientId="$aad_app_id" \
        adminWebAppAadClientId="$admin_aad_app_id" \
        adminWebAppAadDomain="$admin_aad_domain" \
        userWebAppAadClientSecret="$aad_app_secret" \
        adminWebAppAadClientSecret="$admin_aad_app_secret" \
        useCosmosProvisionedThroughput="$p_use_cosmos_provisioned_throughput" \
        turnstileVersion="$TURNSTILE_VERSION" \
        headless="$p_headless" 2>/dev/null

if [[ $? == 0 ]]; then
    echo "✔   Core Turnstile Bicep template successfully deployed.";
else
    # If this template deployment failed, we really are done at this point.
    # We can't do anything more without confirmation that these resources are there.

    echo "❌  Core Turnstile Bicep template deployment failed. See resource group [$resource_group_name] activity log for more information. Setup failed."
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

storage_account_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.storageAccountName.value \
    --output tsv);

storage_account_key=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.storageAccountKey.value \
    --output tsv);

managed_id_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.managedIdId.value \
    --output tsv);

managed_id_name=$(az deployment group show \
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

echo "⚙️   Saving [$deployment_type] deployment profile..."

az storage blob upload \
    --account-name "$storage_account_name" \
    --account-key "$storage_account_key" \
    --container-name "turn-configuration" \
    --data "$deployment_profile" \
    --name "deployment/$deployment_type/profile.json"

echo "🔐   Granting managed identity [$managed_id_name] contributor access to resource group [$resource_group_name]..."

managed_id_sp_id=$(az identity show \
    --ids "$managed_id_id" \
    --query principalId \
    --output tsv)

az role assignment create \
    --assignee "$managed_id_sp_id" \
    --role "Contributor" \
    --scope "/subscriptions/$subscription_id/resourceGroups/$resource_group_name"

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
    echo "🦾   Deploying [$p_integration_pack ($pack_path)] integration pack..."

    az deployment group create \
        --resource-group "$resource_group_name" \
        --name "turn-pack-deploy-$p_deployment_name" \
        --template-file "$pack_path" \
        --parameters \
            deploymentName="$p_deployment_name" \
            managedIdId="$managed_id_id" \
            eventGridConnectionId="$event_grid_connection_id" \
            eventGridConnectionName="$event_grid_connection_name" \
            eventGridTopicId="$event_grid_topic_id" 2>/dev/null

    [[ $? -eq 0 ]] && echo "✔   Integration pack [$p_integration_pack ($pack_path)] deployed.";
    [[ $? -ne 0 ]] && echo "⚠️   Integration pack [$p_integration_pack ($pack_path)] deployment failed."
fi

echo "⚙️   Applying initial publisher configuration..."

if [[ -z $p_publisher_config_path ]]; then
    publisher_config_path="default_publisher_config.json"
else # The user can provide a custom publisher configuration file via the -c switch...
    publisher_config_path=$p_publisher_config_path
fi

az storage blob upload \
    --account-name "$storage_account_name" \
    --account-key "$storage_account_key" \
    --container-name "turn-configuration" \
    --file "$publisher_config_path" \
    --name "publisher_config.json"

echo "🔐   Configuring API access keys..."

# So, why do we do this all the way down here instead of in the bicep template itself? Good question!
# There appears to be some eventually consistent operation that's still completing by the time that
# ARM reports that the deployment of the turn-services-* function app is complete. We deploy the turn-web-* 
# site right after the turn-services-* one is deployed. Turn-web-* requires the function key but,
# since the eventually consistent operation wasn't complete by the time that we tried to create turn-web-*,
# the operation would always fail. We had to inject a temporary wait into the bicep template using a script
# that I was never very happy about. Now that the function app also needs the function key since the EnterTurnstile
# function calls other functions in the same app, this is an even bigger problem because we definitely don't know
# the function key when we're creating the actual function app. Instead, we wait a little bit here after the function
# app deployment, obtain the function app key here in the CLI, then manually push the function key app setting
# to both apps (turn-services-* and turn-web-*) immediately before we publish the actual apps. Basically, we wait
# until the last possible moment to set that setting to allow that eventually consistent operation time to complete.

# Try to get the function key...

for i_function_key in {1..5}; do
    api_key=$(az functionapp keys list \
        --resource-group "$resource_group_name" \
        --name "$api_app_name" \
        --query "functionKeys.default" \
        --output "tsv")

    if [[ -z $api_key || $api_key == null ]]; then
        if [[ $i_function_key == 5 ]]; then
            echo "❌   Failed to obtain Turnstile function app key. Setup failed."
            exit 1     
        else
            sleep_for=$((2**i_function_key))
            echo "⚠️   Trying to obtain function app key again in [$sleep_for] seconds."
            sleep $sleep_for
        fi
    else
        break
    fi
done

# Then add the key to the function app settings...

az functionapp config appsettings set \
    --name "$api_app_name" \
    --resource-group "$resource_group_name" \
    --settings "Turnstile_ApiAccessKey=$api_key"

if [[ "$p_headless" == "$FALSE" ]]; then

    az webapp config appsettings set \
        --name "$web_app_name" \
        --resource-group "$resource_group_name" \
        --settings "Turnstile_ApiAccessKey=$api_key"

    az webapp config appsettings set \
        --name "$admin_web_app_name" \
        --resource-group "$resource_group_name" \
        --settings "Turnstile_ApiAccessKey=$api_key"

fi

# Build and prepare the API and function apps for deployment to the cloud...

echo "🏗️   Building Turnstile app(s)..."

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

echo "🏁   Turnstile deployment complete. It took [$SECONDS] seconds."

if [[ "$p_headless" == "$FALSE" ]]; then
    echo "➡️   Please go to [ $admin_web_app_base_url/config/basics ] to complete setup."
fi

echo






