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
    echo "Turnstile | 0.1-experimental"
    echo "https://github.com/microsoft/turnstile"
    echo
    echo "Copyright (c) Microsoft Corporation. All rights reserved."
    echo "Licensed under the MIT License. See LICENSE in project root for more information."
    echo
    echo "🧪   ⚠️   Experimental; don't use in production (yet.)"
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
    current_user_oid=$(az ad signed-in-user show --query objectId --output tsv 2>/dev/null);
    if [[ -z $current_user_oid ]]; then az login; fi;
done

# Get our parameters...

while getopts "c:d:n:r:" opt; do
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

if [[ -z $p_display_name ]]; then
    display_name="Turnstile $p_deployment_name"
else
    display_name=p_display_name
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
aad_app_secret=$(openssl rand -base64 64)

echo "🛡️   Creating Azure Active Directory (AAD) app [$aad_app_name] registration..."

aad_app_id=$(az ad app create \
    --display-name "$aad_app_name" \
    --available-to-other-tenants true \
    --end-date "2299-12-31" \
    --password "$aad_app_secret" \
    --optional-claims @./aad/manifest.optional_claims.json \
    --required-resource-accesses @./aad/manifest.resource_access.json \
    --app-roles @./aad/manifest.app_roles.json \
    --query appId \
    --output tsv);

echo "🛡️   Creating AAD app [$aad_app_name] service principal..."

sleep 30 # Give AAD a chance to catch up...

aad_sp_id=$(az ad sp create --id "$aad_app_id" --query objectId --output tsv);

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

echo "⚙️   Applying default publisher configuration..."

if [[ -z $p_publisher_config_path ]]; then
    publisher_config_path="default_publisher_config.json"
else # The user can provide a custom publisher configuraiton file via the -c switch...
    publisher_config_path=$p_publisher_config_path
fi

az storage blob upload \
    --account-name "$storage_account_name" \
    --account-key "$storage_account_key" \
    --container-name "configuration" \
    --file "$publisher_config_path" \
    --name "publisher_config.json"

echo "🔐   Adding you to this turnstile's administrative roles..."

# App roles are managed (conveniently) through Microsoft Graph so we first need to get an access token...

graph_token=$(az account get-access-token \
    --resource-type ms-graph \
    --query accessToken \
    --output tsv);

tsa_role_id=$(az ad sp show \
    --id "$aad_sp_id" \
    --query "appRoles[0].id" \
    --output tsv);

ta_role_id=$(az ad sp show \
    --id "$aad_sp_id" \
    --query "appRoles[1].id" \
    --output tsv);

# Add the current user to the subscriber tenant administrator's AAD role...

curl -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $graph_token" \
    -d "{ \"principalId\": \"$current_user_oid\", \"resourceId\": \"$aad_sp_id\", \"appRoleId\": \"$tsa_role_id\" }" \
    "https://graph.microsoft.com/v1.0/users/$current_user_oid/appRoleAssignments"

# Add the current user to the turnstile administrator's AAD role...

curl -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $graph_token" \
    -d "{ \"principalId\": \"$current_user_oid\", \"resourceId\": \"$aad_sp_id\", \"appRoleId\": \"$ta_role_id\" }" \
    "https://graph.microsoft.com/v1.0/users/$current_user_oid/appRoleAssignments"

echo
echo "🛡️   Completing AAD app [$aad_app_name] registration..."

az ad app update \
    --id "$aad_app_id" \
    --reply-urls "$web_app_base_url/signin-oidc";

# Build and prepare the API and function apps for deployment to the cloud...

echo "⚡   Building API function app [$api_app_name]..."

dotnet publish -c Release -o ./api_topublish ../Turnstile.Api/Turnstile.Api.csproj

cd ./api_topublish
zip -r ../api_topublish.zip . >/dev/null
cd ..

echo "🌐   Building web app [$web_app_name]..."

dotnet publish -c Release -o ./web_topublish ../Turnstile.Web/Turnstile.Web.csproj

cd ./web_topublish
zip -r ../web_topublish.zip . >/dev/null
cd ..

echo "☁️    Publishing API function app [$api_app_name]..."

az functionapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --src "./api_topublish.zip"

echo "☁️    Publishing web app [$web_app_name]..."

az webapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$web_app_name" \
    --src "./web_topublish.zip"

echo "🧹   Cleaning up..."

rm -rf ./api_topublish >/dev/null
rm -rf ./api_topublish.zip >/dev/null
rm -rf ./web_topublish >/dev/null
rm -rf ./web_topublish.zip >/dev/null

echo "🏁   Turnstile deployment complete. It took [$SECONDS] seconds."
echo
echo "➡️   Please go to [ $web_app_base_url/publisher/setup ] to complete setup."






