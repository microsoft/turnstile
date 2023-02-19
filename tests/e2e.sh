#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

SECONDS=0 # Let's time it

turnstile_version=$(cat ../VERSION) # TODO: Add code to auotomatically roll this forward on PR.

test_run_id=$(date +%s) # Test run ID is Unix epoch time. We'll use this as an identifier for the resources that we 
                        # stand up in Azure to run these tests a little later.

usage() { 
  echo "Usage:   $0 <-r azure_region> [-k]"
  echo "Example: $0 -r \"southcentralus\" -k"
  echo
  echo "<-r azure_region>.......The Azure region in which these tests will be run"
  echo "                        For region list, run `az account list-locations --output table`"
  echo "[-k]....................Optional flag; retains the test resource group"
  echo "                        If not set, the resource group will be automatically deleted"
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
        echo "❌   [$region] is not a valid Azure region, but these are..."
        echo
        az account list-locations --output table --query "[].name"
        echo
        return 1
    else
        echo "✔   [$region] is a valid Azure region ($region_display_name)."
    fi
}

check_turnstile_health() {
    local test_run_id="$1"
    local api_app_name="$2"

    local health_status=""

    for i in {1..6}; do
        sleep 10

        health_status=$(curl -s -o /dev/null -w "%{http_code}" "https://$api_app_name.azurewebsites.net/health")

        echo "🩺   Checking Turnstile test environment [$deployment_name] health (attempt $i of 6)..."

        if [[ $health_status == "200" ]]; then
            echo "✔   Turnstile test environment [$deployment_name] is healthy (HTTP $health_status)!"
            return 0 # All good!
        fi
    done

     # If we got this far, something's definitely not right...

    echo "⚠️   Turnstile test environment [$test_run_id] is unhealthy (HTTP $health_status)."
    return 1
}

splash() {
    echo "Turnstile | $turnstile_version"
    echo "https://github.com/microsoft/turnstile"
    echo
    echo "🧪   End-to-end test runner"
    echo
    echo "Copyright (c) Microsoft Corporation. All rights reserved."
    echo "Licensed under the MIT License. See LICENSE in project root for more information."
    echo
}

# Howdy!

splash

# Make sure all pre-reqs are installed...

echo "Checking prerequisites..."

check_az;           [[ $? -ne 0 ]] && prereq_check_failed=1
check_dotnet;       [[ $? -ne 0 ]] && prereq_check_failed=1

if [[ -z $prereq_check_failed ]]; then
    echo "✔   All prerequisites installed."
else
    echo "❌   Please install all prerequisites then try again."
    return 1
fi

# Log in the user if they aren't already...

while [[ -z $current_user_oid ]]; do
    current_user_oid=$(az ad signed-in-user show --query id --output tsv 2>/dev/null);
    if [[ -z $current_user_oid ]]; then az login; fi;
done

while getopts "r:k" opt; do
    case $opt in
        r)
            p_region=$OPTARG
        ;;
        k)
            p_keep=1 
        ;;
        \?)
            usage
            exit 1
        ;;
    esac
done

check_deployment_region "$p_region"

[[ $? == 0 ]] || exit 1 # The region is invalid and an error message has already been displayed. We're done here.

resource_group_name="turn-e2e-test-$test_run_id"

if [[ $(az group exists --resource-group "$resource_group_name" --output tsv) == false ]]; then
    echo "Creating resource group [$resource_group_name]..."

    az group create \
        --location "$p_region" \
        --name "$resource_group_name" \
        --tags \
            "Turnstile Deployment Name"="$test_run_id" \
            "Turnstile Version"="$turnstile_version"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
fi

if [[ -z $p_keep ]]; then 
    echo "ℹ️   -k flag not set. Resource group [$resource_group_name] will be deleted once testing is complete."
else
    echo "ℹ️   -k flag set. Resource group [$resource_group_name] will remain once testing is complete."
fi

az_deployment_name="turn-e2e-test-$test_run_id-deploy"

echo "🦾   Deploying test enviroment into resource group [$resource_group_name]..."

az deployment group create \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --template-file "./test_environment.bicep" \
    --parameters \
        deploymentName="$test_run_id"

# We're going to need these variables here in a bit...

api_app_id=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.apiAppId.value \
    --output tsv);

api_app_name=$(az deployment group show \
    --resource-group "$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.apiAppName.value \
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

echo "🏗️   Building Turnstile API app..."

dotnet publish -c Release -o ./api_topublish ../Turnstile/Turnstile.Api/Turnstile.Api.csproj

cd ./api_topublish
zip -r ../api_topublish.zip . >/dev/null
cd ..

echo "⚙️   Applying test Turnstile publisher configuration..."

az storage blob upload \
    --account-name "$storage_account_name" \
    --account-key "$storage_account_key" \
    --container-name "turn-configuration" \
    --file "./test_publisher_config.json" \
    --name "publisher_config.json"

echo "☁️    Publishing Turnstile API app..."

az functionapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --src "./api_topublish.zip"

echo "🔌   Connecting Turnstile API event store function (PostEventToStore) to event grid..."

az eventgrid event-subscription create \
    --name "event-store-connection" \
    --source-resource-id "$event_grid_topic_id" \
    --endpoint "$api_app_id/functions/PostEventToStore" \
    --endpoint-type azurefunction

echo "🔑   Getting Turnstile API function key..."

api_key=$(az functionapp keys list \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --query "functionKeys.default" \
    --output "tsv")

az functionapp config appsettings set \
    --name "$api_app_name" \
    --resource-group "$resource_group_name" \
    --settings "Turnstile_ApiAccessKey=$api_key"

# Wait for Turnstile to wake up...

check_turnstile_health "$test_run_id" "$api_app_name"

if [[ $? != 0 ]]; then
    echo "❌   Failed to create test environment [$test_run_id]. Testing failed."
    exit 1
fi

echo
echo "🧪   Running tests..."

api_base_url="https://$api_app_name.azurewebsites.net/api"

chmod +x ./e2e_core_api.sh
./e2e_core_api.sh "$api_base_url" "$api_key" "$storage_account_name" "$storage_account_key"
[[ $? == 0 ]] || tests_failed=1

chmod +x ./e2e_entry_api.sh
./e2e_entry_api.sh "$api_base_url" "$api_key"
[[ $? == 0 ]] || tests_failed=1

echo
echo "🧹   Cleaning up..."

if [[ -z $p_keep ]]; then # User can optionally keep the reource group by setting "keep" or "k" flag
    az group delete --yes -g "$resource_group_name"
fi

rm -rf ./api_topublish
rm -rf ./api_topublish.zip

echo
echo "Testing took [$SECONDS] seconds."
echo

if [[ -z $tests_failed ]]; then
    echo ""
    exit 0 # All of our tests passed.
else
    exit 1 # Some of our tests failed.
fi
