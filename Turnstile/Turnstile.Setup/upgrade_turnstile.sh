#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

THIS_TURNSTILE_VERSION=$(cat ../../VERSION)

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

check_prereqs() {
    echo "Checking Turnstile upgrade prerequisites...";

    check_az;       if [[ $? -ne 0 ]]; then prereq_check_failed=1; fi;
    check_dotnet;   if [[ $? -ne 0 ]]; then prereq_check_failed=1; fi;

    if [[ -z $prereq_check_failed ]]; then
        echo "✔   All Turnstile upgrade prerequisites satisfied."
    else
        return 1
    fi
}

upgrade_turn_rg() {
    local subscription_id="$1"
    local rg_name="$2"
    local deployment_name="$3"

    local api_app_name="turn-services-$deployment_name"
    local web_app_name="turn-web-$deployment_name"

    local api_app_id=$(az resource show \
        --resource-group "$rg_name" \
        --resource-type "Microsoft.Web/sites" \
        --subscription "$subscription_id" \
        --name "$api_app_name" \
        --query "id" \
        --output "tsv")

    local web_app_id=$(az resource show \
        --resource-group "$rg_name" \
        --resource-type "Microsoft.Web/sites" \
        --subscription "$subscription_id" \
        --name "$web_app_name" \
        --query "id" \
        --output "tsv")

    if [[ -z "$api_app_id" || -z "$web_app_id" ]]; then

        # There should be an API and web app here but there isn't. Let the user know then bail early...

        echo
        echo "⚠️   Expected app services [$api_app_name] and [$web_app_name] not found in resource group [$rg_name]. Upgrade failed." >&2

        return 1
    else
        # Alright so we're doing this...

        local upgrade_slot_name="turn-upgrade-$(date +%s)"

        # Create a temporary slot to push the new Turnstile web app to...

        echo
        echo "🏗️   Creating app service [$web_app_name] temporary upgrade slot [$upgrade_slot_name]..."

        az webapp deployment slot create \
            --name "$web_app_name" \
            --resource-group "$rg_name" \
            --slot "$upgrade_slot_name" \
            --configuration-source "$web_app_name" \
            --subscription "$subscription_id"

        web_slot_created=$?

        echo
        echo "🏗️   Creating app service [$api_app_name] temporary upgrade slot [$upgrade_slot_name]..."

        az functionapp deployment slot create \
            --name "$api_app_name" \
            --resource-group "$rg_name" \
            --slot "$upgrade_slot_name" \
            --configuration-source "$api_app_name" \
            --subscription "$subscription_id"

        api_slot_created=$?

        if [[ $web_slot_created -ne 0 || $api_slot_created -ne 0 ]]; then
            # We couldn't create deployment slots which means that we can't do this upgrade
            # safely which means that we're not going to try to do it at all. I mean, this thing
            # is probably running in production. More than likely, the app service that Turnstile
            # is deployed to doesn't support deployment slots (< Standard).

            echo
            echo "⚠️  Unable to create temporary upgrade slots. Can not safely perform upgrade. Please ensure that your App Service Plan SKU is Standard (S1) or higher. For more information, see [ https://docs.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#app-service-limits ]."
            
            return 1
        fi

        # Alright, let's build the new version of Turnstile...

        echo
        echo "📦   Packaging new Turnstile web app for deployment to app service [$web_app_name] temporary upgrade slot [$upgrade_slot_name]..."

        dotnet publish -c Release -o ./web_topublish ../Turnstile.Web/Turnstile.Web.csproj
        cd ./web_topublish
        zip -r ../web_topublish.zip . >/dev/null
        cd ..

        echo
        echo "📦   Packaging new Turnstile API app for deployment to app service [$api_app_name] temporary upgrade slot [$upgrade_slot_name]..."

        dotnet publish -c Release -o ./api_topublish ../Turnstile.Api/Turnstile.Api.csproj
        cd ./api_topublish
        zip -r ../api_topublish.zip . >/dev/null
        cd ..

        echo
        echo "☁️   Deploying upgraded Turnstile web app to app service [$web_app_name] temporary upgrade slot [$upgrade_slot_name]..."

        az webapp deployment source config-zip \
            --src ./web_topublish.zip \
            --name "$web_app_name" \
            --resource-group "$rg_name" \
            --slot "$upgrade_slot_name" \
            --subscription "$subscription_id"

        echo
        echo "☁️   Deploying upgraded Turnstile API app to app service [$web_app_name] temporary upgrade slot [$upgrade_slot_name]..."

        az functionapp deployment source config-zip \
            --src ./api_topublish.zip \
            --name "$api_app_name" \
            --resource-group "$rg_name" \
            --slot "$upgrade_slot_name" \
            --subscription "$subscription_id"

        # Clean up after ourselves...

        rm -rf ./api_topublish >/dev/null
        rm -rf ./api_topublish.zip >/dev/null
        rm -rf ./web_topublish >/dev/null
        rm -rf ./web_topublish.zip >/dev/null

        echo "🔃   Upgraded Turnstile API and web apps deployed to slots [$api_app_name/$upgrade_slot_name] and [$web_app_name/$upgrade_slot_name]. Swapping production slots with upgraded deployment slots..."

        az functionapp deployment slot swap \
            --slot "$upgrade_slot_name" \
            --action "swap" \
            --name "$api_app_name" \
            --resource-group "$rg_name" \
            --subscription "$subscription_id" \
            --target-slot "production" &

        api_swap_pid=$!

        az webapp deployment slot swap \
            --slot "$upgrade_slot_name" \
            --action "swap" \
            --name "$web_app_name" \
            --resource-group "$rg_name" \
            --subscription "$subscription_id" \
            --target-slot "production" &

        web_swap_pid=$!

        wait $api_swap_pid
        wait $web_swap_pid

        echo
        echo "✔   Upgraded Turnstile API and web apps promoted to production slots."

        # TODO: Check health here. For now, we need to confirm manually. Don't worry, health check endpoint coming soon!

        echo "Please take a moment to confirm that Turnstile is operating as expected."

        read -p "❔   Complete Turnstile upgrade? [y/N] " complete_upgrade

        case "$complete_upgrade" in
            [yY1]   ) 
                echo "✔   Upgrade successful."
                commit_upgrade      "$upgrade_slot_name" "$api_app_name" "$web_app_name" "$rg_name" "$subscription_id"
            ;;
            *       )
                # Bummer :/
                echo "❌  Upgrade failed."
                rollback_upgrade    "$upgrade_slot_name" "$api_app_name" "$web_app_name" "$rg_name" "$subscription_id"
            ;;
        esac
    fi
}

commit_upgrade() {
    local upgrade_slot_name=$1
    local api_app_name=$2
    local web_app_name=$3
    local rg_name=$4
    local subscription_id=$5

    echo
    echo "🏷️   Tagging Turnstile resource group [$rg_name] with updated Turnstile version..."

    az tag update \
        --resource-id "/subscriptions/$subscription_id/resourcegroups/$rg_name" \
        --operation "merge" \
        --tags "Turnstile Version"="$THIS_TURNSTILE_VERSION" \

    echo
    echo "🧹   Deleting app service [$api_app_name] and [$web_app_name] temporary upgrade slots [$upgrade_slot_name]..."

    az functionapp deployment slot delete \
        --slot "$upgrade_slot_name" \
        --name "$api_app_name" \
        --resource-group "$rg_name" \
        --subscription "$subscription_id"

    delete_api_slot_pid=$!

    az webapp deployment slot delete \
        --slot "$upgrade_slot_name" \
        --name "$web_app_name" \
        --resource-group "$rg_name" \
        --subscription "$subscription_id"

    delete_web_slot_pid=$!

    wait $delete_api_slot_pid
    wait $delete_web_slot_pid

    echo
    echo "✔   Upgrade to [$THIS_TURNSTILE_VERSION] complete."
}

rollback_upgrade() {
    local upgrade_slot_name=$1
    local api_app_name=$2
    local web_app_name=$3
    local rg_name=$4
    local subscription_id=$5

    echo
    echo "🔙  Rolling Turnstile back to previous version..."

    az functionapp deployment slot swap \
        --slot "$upgrade_slot_name" \
        --action "swap" \
        --name "$api_app_name" \
        --resource-group "$rg_name" \
        --subscription "$subscription_id" \
        --target-slot "production"

    api_rollback_pid=$!

    az webapp deployment slot swap \
        --slot "$upgrade_slot_name" \
        --action "swap" \
        --name "$web_app_name" \
        --resource-group "$rg_name" \
        --subscription "$subscription_id" \
        --target-slot "production"

    web_rollback_pid=$!

    wait $api_rollback_pid
    wait $web_rollback_pid

    echo
    echo "🪲   Leaving unhealthy Turnstile deployments in slot [$upgrade_slot_name] for debugging purposes."
}

cat ./splash.txt
echo

check_prereqs

if [[ $? -ne 0 ]]; then
    echo "❌   Please install all Turnstile setup prerequisites then try again. Setup failed."
    exit 1
fi

echo
echo "🔍   Scanning accessible subscriptions for upgradeable Turnstile deployments..."

subscription_ids=$(az account subscription list \
    --query "[].subscriptionId" \
    --output "tsv")

for subscription_id in $subscription_ids; do

    echo "🔍   Scanning subscription [$subscription_id]..."

    # If a resource group has a "Turnstile Version" tag, we can be pretty confident 
    # that it's a Turnstile deployment. Get all the resource groups that we can touch
    # that probably contain a Turnstile deployment.

    turn_rg_names=$(az group list \
        --subscription "$subscription_id" \
        --query "[?tags.\"Turnstile Version\" != null].name" \
        --output "tsv")

    for turn_rg_name in $turn_rg_names; do

        # Get this resource group's Turnstile version.

        rg_turn_version=$(az group show \
            --name "$turn_rg_name" \
            --subscription "$subscription_id" \
            --query "tags.\"Turnstile Version\"" \
            --output "tsv")

        # Get this resource group's Turnstile deployment name.

        rg_turn_name=$(az group show \
            --name "$turn_rg_name" \
            --subscription "$subscription_id" \
            --query "tags.\"Turnstile Deployment Name\"" \
            --output "tsv")

        if [[ -z $rg_turn_name ]]; then

            # Well, this is awkward. Somehow, we got in a situation where we have a resource group that 
            # presumably contains a Turnstile deployment but doesn't have a "Turnstile Deployment Name" tag. We need to 
            # know the name of the deployment in order to upgrade it so we'll have to skip this one. Bummer.

            echo "⚠️   Azure resource group [$turn_rg_name (subscription: $subscription_id)] is tagged with Turnstile version [$rg_turn_version] but has no \"Turnstile Deployment Name\" tag. Unable to upgrade Turnstile without a deployment name." >&2

        elif [[ "$THIS_TURNSTILE_VERSION" != "$rg_turn_version" ]]; then # Different Turnstile version so potentially upgradeable.

            # Comparing semantic versions is difficult and gets even fuzzier when you're trying to compare
            # versions that contain suffixes like -prerelease. I was thinking about specifically breaking out 
            # the version number into resource group tags (a major, minor, and patch tag) but, for now, we'll
            # just have the user confirm that they want to upgrade a deployment if the version number is different
            # than the current version number.

            echo
            echo "❕   Potentially upgradeable Turnstile deployment found."
            echo
            echo "Deployment Name:      [$rg_turn_name]"
            echo "Azure Subscription:   [$subscription_id]"
            echo "Resouce Group:        [$turn_rg_name]"
            echo
            echo "Current Version:      [$rg_turn_version]"
            echo "Upgrade to Version?:  [$THIS_TURNSTILE_VERSION]"
            echo

            read -p "❔   Upgrade Turnstile deployment [$rg_turn_name] to version [$THIS_TURNSTILE_VERSION]? [y/N] " initiate_upgrade

            case "$initiate_upgrade" in
                [yY1]   ) 
                    upgrade_turn_rg "$subscription_id" "$turn_rg_name" "$rg_turn_name" # We have a winner!

                    echo
                    read -p "❔   Keep scanning for upgradeable Turnstile deployments? [y/N] " keep_scanning

                    case "$keep_scanning" in
                        [yY1]   ) ;;
                        *       ) exit 0 ;;
                    esac
                ;;
                *       )
                ;; # Move along now. Nothing to see here...
            esac
        else
            echo "Turnstile deployment [$rg_turn_name (subscription: $subscription_id; resource group: $turn_rg_name)] is already version [$THIS_TURNSTILE_VERSION]."
        fi
    done
done

