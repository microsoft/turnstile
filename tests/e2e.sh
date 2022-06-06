#!/bin/bash

SECONDS=0 # Let's time it

all_passed=0 # We use this variable to roll up all the test results. If we get done running this script
             # and this value is still 0, all of our tests passed. If it's anything else, at least one of
             # the tests has failed.

turnstile_version="0.1-prerelease" # TODO: Add code to auotomatically roll this forward on PR.

test_location=$1 # For simplicity, this script only takes one parameter - the Azure region where the tests should be run.
                 # We'll be blowing this whole thing away when we're done testing so I don't see much reason to 
                 # customize the deployment further (at this point at least.)

test_run_id=$(date +%s) # Test run ID is Unix epoch time. We'll use this as an identifier for the resources that we 
                        # stand up in Azure to run these tests a little later.

usage() { echo "Usage: $0 <azure_region>"; }

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

# Check that the region the user provided is valid...

[[ -z "$test_location" ]] && { usage; exit 1; }

check_deployment_region "$test_location"

[[ $? == 0 ]] || exit 1 # The region is invalid and an error message has already been displayed. We're done here.

resource_group_name="turn-e2e-test-$test_run_id"

if [[ $(az group exists --resource-group "$resource_group_name" --output tsv) == false ]]; then
    echo "Creating resource group [$resource_group_name]..."

    az group create --location "$test_location" --name "$resource_group_name"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
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

topic_id=$(az deployment group show \
    --resource-group="$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.topicId.value \
    --output tsv);

echo "🏗️   Building Turnstile API app..."

dotnet publish -c Release -o ./api_topublish ../Turnstile/Turnstile.Api/Turnstile.Api.csproj

cd ./api_topublish
zip -r ../api_topublish.zip . >/dev/null
cd ..

echo "☁️    Publishing Turnstile API app..."

az functionapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --src "./api_topublish.zip"

echo "🔌   Connecting Turnstile API event store function (PostEventToStore) to event grid..."

az eventgrid event-subscription create \
    --name "event-store-connection" \
    --source-resource-id "$topic_id" \
    --endpoint "$api_app_id/functions/PostEventToStore" \
    --endpoint-type azurefunction

echo "🔑   Getting Turnstile API function key..."

api_key=$(az functionapp keys list \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --query "functionKeys.default" \
    --output "tsv")

echo "🧪   Running tests..."

api_url="https://$api_app_name.azurewebsites.net"

run_can_create_subscription "$api_url" "$api_key"

echo "🧹   Cleaning up..."

az group delete --yes -g "$resource_group_name"

rm -rf ./api_topublish
rm -rf ./api_topublish.zip

echo "Testing took [$SECONDS] seconds."
echo

# Each test is implemented as a method down here...

run_can_create_subscription() {
    api_url=$1
    api_key=$2

    chmod +x ./can_create_subscription/run_test.sh
    ./can_create_subscription/run_test.sh "$api_url" "$api_key"
    
    if [[ $? != 0 ]]; then 
        all_passed=1 # This test run has failed.
    fi
}
