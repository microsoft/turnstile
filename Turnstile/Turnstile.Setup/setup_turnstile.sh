#!/bin/bash

SECONDS=0 # Let's time it...

turnstile_version="0.1-experimental"

usage() { echo "Usage: $0 <-n name> <-r deployment_region> [-c publisher_config_path] [-d display_name] [-i integration_pack]"; }

check_az() {
    az version >/dev/null

    if [[ $? -ne 0 ]]; then
        echo "❌   Please install the Azure CLI before continuing. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
        return 1
    else
        echo "✔   Azure CLI installed."
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

check_deployment_region() {
    region=$1

    region_display_name=$(az account list-locations -o tsv --query "[?name=='$region'].displayName")

    if [[ -z $region_display_name ]]; then
        echo "❌   [$region] is not a valid Azure region. For a full list of Azure regions, run 'az account list-locations -o table'."
        return 1
    else
        echo "✔   [$region] is a valid Azure region ($region_display_name)."
    fi
}

check_deployment_name() {
    name=$1

    if [[ $name =~ ^[a-z0-9]{5,13}$ ]]; then
        echo "✔   [$name] is a valid Turnstile deployment name."
    else
        echo "❌   [$name] is not a valid Turnstile deployment name. The name must contain only lowercase letters and numbers and be between 5 and 13 characters in length."
        return 1
    fi
}

splash() {
    echo "Turnstile | $turnstile_version"
    echo "https://github.com/microsoft/turnstile"
    echo
    echo "Copyright (c) Microsoft Corporation. All rights reserved."
    echo "Licensed under the MIT License. See LICENSE in project root for more information."
    echo
}

# Howdy!

splash

# Make sure all pre-reqs are installed...

echo "Checking setup prerequisites..."

check_az;           [[ $? -ne 0 ]] && prereq_check_failed=1
check_dotnet;       [[ $? -ne 0 ]] && prereq_check_failed=1

if [[ -z $prereq_check_failed ]]; then
    echo "✔   All setup prerequisites installed."
else
    echo "❌   Please install all setup prerequisites then try again."
    return 1
fi

# Log in the user if they aren't already...

while [[ -z $current_user_oid ]]; do
    current_user_oid=$(az ad signed-in-user show --query id --output tsv 2>/dev/null);
    if [[ -z $current_user_oid ]]; then az login; fi;
done

# Get our parameters...

p_integration_pack="default"

while getopts "c:d:n:r:i:" opt; do
    case $opt in
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
        \?)
            usage
            exit 1
        ;;
    esac
done

echo "Validating script parameters..."

[[ -z p_deployment_name || -z p_deployment_region ]] && { usage; exit 1; }

check_deployment_region $p_deployment_region;   [[ $? -ne 0 ]] && param_check_failed=1
check_deployment_name $p_deployment_name;       [[ $? -ne 0 ]] && param_check_failed=1

if [[ -z $param_check_failed ]]; then
    echo "✔   All setup parameters are valid."
else
    echo "❌   Parameter validation failed. Please review and try again."
    return 1
fi

p_deployment_name=$(echo "$p_deployment_name" | tr '[:upper:]' '[:lower:]') # Lower the deployment name...

if [[ -z $p_display_name ]]; then
    display_name="Turnstile $p_deployment_name"
else
    display_name="$p_display_name"
fi

# Create our resource group if it doesn't already exist...

resource_group_name="turnstile-$p_deployment_name"

if [[ $(az group exists --resource-group "$resource_group_name" --output tsv) == false ]]; then
    echo "Creating resource group [$resource_group_name]..."

    az group create --location "$p_deployment_region" --name "$resource_group_name"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
fi

# Create the app registration in AAD...

aad_app_name="$display_name"

echo "🛡️   Creating Azure Active Directory (AAD) app [$aad_app_name] registration..."

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

create_app_response=$(curl \
    -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $graph_token" \
    -d "$create_app_json" \
    "https://graph.microsoft.com/v1.0/applications")

sleep 15 # Give AAD a chance to catch up...

aad_object_id=$(echo "$create_app_response" | jq -r ".id")
aad_app_id=$(echo "$create_app_response" | jq -r ".appId")
add_password_json=$(cat ./aad/add_password.json)

add_password_response=$(curl \
    -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $graph_token" \
    -d "$add_password_json" \
    "https://graph.microsoft.com/v1.0/applications/$aad_object_id/addPassword")

aad_app_secret=$(echo "$add_password_response" | jq -r ".secretText")

echo "🛡️   Creating AAD app [$aad_app_name] service principal..."

sleep 30 # Give AAD a chance to catch up...

aad_sp_id=$(az ad sp create --id "$aad_app_id" --query id --output tsv);

if [[ -z $aad_sp_id ]]; then
    echo "$lp ❌   Unable to create service principal for AAD app [$aad_app_name ($aad_app_id)]. See above output for details. Setup failed."
    exit 1
fi

echo "🔐   Granting AAD app [$aad_app_name] service principal [$aad_sp_id] contributor access to resource group [$resource_group_name]..."

sleep 30 # Give AAD a chance to catch up...

az role assignment create \
    --role "Contributor" \
    --assignee "$aad_sp_id" \
    --resource-group "$resource_group_name"

subscription_id=$(az account show --query id --output tsv);
current_user_tid=$(az account show --query tenantId --output tsv);
az_deployment_name="turnstile-deploy-$p_deployment_name"

echo "🦾   Deploying Turnstile Bicep template to subscription [$subscription_id] resource group [$resource_group_name]..."

az deployment group create \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --template-file "./turnstile_deploy.bicep" \
    --parameters \
        deploymentName="$p_deployment_name" \
        webAppAadClientId="$aad_app_id" \
        webAppAadTenantId="$current_user_tid" \
        webAppAadClientSecret="$aad_app_secret"

api_app_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.apiAppName.value \
    --output tsv);

web_app_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.webAppName.value \
    --output tsv);

web_app_base_url=$(az deployment group show \
    --resource-group="$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.webAppBaseUrl.value \
    --output tsv);

storage_account_name=$(az deployment group show \
    --resource-group="$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.storageAccountName.value \
    --output tsv);

storage_account_key=$(az deployment group show \
    --resource-group="$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.storageAccountKey.value \
    --output tsv);

topic_name=$(az deployment group show \
    --resource-group="$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.topicName.value \
    --output tsv);

# Deploy integration pack.

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
    echo "Deploying [$p_integration_pack ($pack_path)] integration pack..."

    az deployment group create \
        --resource-group "$resource_group_name" \
        --name "turn-pack-deploy-$p_deployment_name" \
        --template-file "$pack_path" \
        --parameters \
            deploymentName="$p_deployment_name"

    [[ $? -eq 0 ]] && echo "$lp ✔   Integration pack [$p_integration_pack ($pack_path)] deployed.";
    [[ $? -ne 0 ]] && echo "$lp ⚠️   Integration pack [$p_integration_pack ($pack_path)] deployment failed."
fi

echo "⚙️   Applying default publisher configuration..."

if [[ -z $p_publisher_config_path ]]; then
    publisher_config_path="default_publisher_config.json"
else # The user can provide a custom publisher configuraiton file via the -c switch...
    publisher_config_path=$p_publisher_config_path
fi

az storage blob upload \
    --account-name "$storage_account_name" \
    --account-key "$storage_account_key" \
    --container-name "turn-configuration" \
    --file "$publisher_config_path" \
    --name "publisher_config.json"

echo "🔐   Adding you to this turnstile's administrative roles..."

# Add the current user to the subscriber tenant administrator's AAD role...

curl -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $graph_token" \
    -d "{ \"principalId\": \"$current_user_oid\", \"resourceId\": \"$aad_sp_id\", \"appRoleId\": \"$tenant_admin_role_id\" }" \
    "https://graph.microsoft.com/v1.0/users/$current_user_oid/appRoleAssignments" &

tenant_admin_role_pid=$!

# Add the current user to the turnstile administrator's AAD role...

curl -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $graph_token" \
    -d "{ \"principalId\": \"$current_user_oid\", \"resourceId\": \"$aad_sp_id\", \"appRoleId\": \"$turnstile_admin_role_id\" }" \
    "https://graph.microsoft.com/v1.0/users/$current_user_oid/appRoleAssignments" &

turnstile_admin_role_pid=$!

wait $tenant_admin_role_pid
wait $turnstile_admin_role_pid

# Build and prepare the API and function apps for deployment to the cloud...

echo "🏗️   Building Turnstile API and web apps..."

# Save a little time and run the API and web app builds in parallel...

dotnet publish -c Release -o ./api_topublish ../Turnstile.Api/Turnstile.Api.csproj &

build_api_pid=$!

dotnet publish -c Release -o ./web_topublish ../Turnstile.Web/Turnstile.Web.csproj &

build_web_pid=$!

# Once the builds are finished, pack them up for deployment.

wait $build_api_pid

cd ./api_topublish
zip -r ../api_topublish.zip . >/dev/null
cd ..

wait $build_web_pid

cd ./web_topublish
zip -r ../web_topublish.zip . >/dev/null
cd ..

echo "☁️    Publishing Turnstile API and web apps..."

# We can also run the deployments in parallel...

az functionapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --src "./api_topublish.zip" &

deploy_api_pid=$!

az webapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$web_app_name" \
    --src "./web_topublish.zip" &

deploy_web_pid=$!

# Wait for both deployments to finish...

wait $deploy_api_pid
wait $deploy_web_pid

echo "🧹   Cleaning up..."

rm -rf ./api_topublish >/dev/null
rm -rf ./api_topublish.zip >/dev/null
rm -rf ./web_topublish >/dev/null
rm -rf ./web_topublish.zip >/dev/null

echo "🏁   Turnstile deployment complete. It took [$SECONDS] seconds."
echo "➡️   Please go to [ $web_app_base_url/publisher/setup ] to complete setup."
echo






