#!/bin/bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

SECONDS=0 # Let's time it...

turnstile_version=$(cat ../../VERSION)

usage() { echo "Usage: $0 <-n name> <-r deployment_region> [-c publisher_config_path] [-d display_name] [-i integration_pack] [-h flag: headless]"; }

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

# Try to upgrade Bicep...

az bicep upgrade

# Log in the user if they aren't already...

while [[ -z $current_user_oid ]]; do
    current_user_oid=$(az ad signed-in-user show --query id --output tsv 2>/dev/null);
    if [[ -z $current_user_oid ]]; then az login; fi;
done

# Get our parameters...

p_integration_pack="default"

while getopts "c:d:n:r:i:h" opt; do
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
        h)
            p_headless=1

            # This flag allows you to deploy Turnstile in "headless" mode. Headless mode deploys _only_
            # the API and supporting resources. It does not deploy the web app and, consequently, doesn't
            # do any AAD configuration.
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

    az group create \
        --location "$p_deployment_region" \
        --name "$resource_group_name" \
        --tags \
            "Turnstile Deployment Name"="$p_deployment_name" \
            "Turnstile Version"="$turnstile_version"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
fi

# If we're deploying in headless mode, we can skip all these Azure AD shenanigans...

if [[ -z $p_headless ]]; then

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

        aad_object_id=$(echo "$create_app_response" | jq -r ".id")
        aad_app_id=$(echo "$create_app_response" | jq -r ".appId")

        if [[ -z $aad_object_id || -z $aad_app_id || $aad_object_id == null || $aad_app_id == null ]]; then
            if [[ $i1 == 5 ]]; then
                # We tried five times and it's still not working. Time to give up, unfortunately.
                echo "❌   Failed to create Turnstile AAD app. Setup failed."
                exit 1
            else
                sleep_for=$((2**i1)) # Exponential backoff - 2..4..8..16..32 second wait between retries
                echo "⚠️   Trying to create app again in [$sleep_for] seconds."
                sleep $sleep_for
            fi
        else
            break
        fi
    done

    echo "🛡️   Creating Azure Active Directory (AAD) app [$aad_app_name] client credentials..."

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
                echo "❌   Failed to create Turnstile AAD app client credentials. Setup failed."
                exit 1
            else
                sleep_for=$((2**i2))
                echo "⚠️   Trying to create app client credentials again in [$sleep_for] seconds."
                sleep $sleep_for
            fi
        else
            break
        fi
    done

    echo "🛡️   Creating AAD app [$aad_app_name] service principal..."

    for i3 in {1..5}; do
        aad_sp_id=$(az ad sp create --id "$aad_app_id" --query id --output tsv)

        if [[ -z $aad_sp_id || $aad_sp_id == null ]]; then
            if [[ $i3 == 5 ]]; then
                echo "❌   Failed to create Turnstile AAD service principal. Setup failed."
                exit 1
            else
                sleep_for=$((2**i2))
                echo "⚠️   Trying to create service principal again in [$sleep_for] seconds."
                sleep $sleep_for
            fi     
        else
            break
        fi
    done

    echo "🔐   Granting AAD app [$aad_app_name] service principal [$aad_sp_id] contributor access to resource group [$resource_group_name]..."

    for i4 in {1..5}; do
        az role assignment create \
            --role "Contributor" \
            --assignee "$aad_sp_id" \
            --resource-group "$resource_group_name"

        if [[ $? != 0 ]]; then
            if [[ $i4 == 5 ]]; then
                echo "❌   Failed to create Turnstile AAD service principal. Setup failed."
                exit 1     
            else
                sleep_for=$((2**i4))
                echo "⚠️   Trying to create service principal again in [$sleep_for] seconds."
                sleep $sleep_for
            fi
        else
            break
        fi
    done

fi

subscription_id=$(az account show --query id --output tsv);
current_user_tid=$(az account show --query tenantId --output tsv);
az_deployment_name="turnstile-deploy-$p_deployment_name"

echo "🦾   Deploying Turnstile Bicep template to subscription [$subscription_id] resource group [$resource_group_name]..."

if [[ -z $p_headless ]]; then

    az deployment group create \
        --resource-group "$resource_group_name" \
        --name "$az_deployment_name" \
        --template-file "./turnstile_deploy.bicep" \
        --parameters \
            deploymentName="$p_deployment_name" \
            webAppAadClientId="$aad_app_id" \
            webAppAadTenantId="$current_user_tid" \
            webAppAadClientSecret="$aad_app_secret" \
            headless="false"

    # web_app_name and web_app_base_url are only provided when
    # not deploying in headless mode.

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

else

    az deployment group create \
        --resource-group "$resource_group_name" \
        --name "$az_deployment_name" \
        --template-file "./turnstile_deploy.bicep" \
        --parameters \
            deploymentName="$p_deployment_name" \
            headless="true"

fi

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

topic_name=$(az deployment group show \
    --resource-group="$resource_group_name" \
    --name "$az_deployment_name" \
    --query properties.outputs.topicName.value \
    --output tsv);

if [[ -z $p_headless ]]; then

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
                deploymentName="$p_deployment_name"

        [[ $? -eq 0 ]] && echo "$lp ✔   Integration pack [$p_integration_pack ($pack_path)] deployed.";
        [[ $? -ne 0 ]] && echo "$lp ⚠️   Integration pack [$p_integration_pack ($pack_path)] deployment failed."
    fi

fi

echo "⚙️   Applying default publisher configuration..."

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

if [[ -z $p_headless ]]; then

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

    echo

fi

echo "🔐   Configuring API access keys..."

# So, why do we do this all the way down here instead of in the bicep template itself? Good question!
# There appears to be some eventually consistent operation that't still completing by the time that
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

if [[ -z $p_headless ]]; then

    az webapp config appsettings set \
        --name "$web_app_name" \
        --resource-group "$resource_group_name" \
        --settings "Turnstile_ApiAccessKey=$api_key"

fi

# Build and prepare the API and function apps for deployment to the cloud...

echo "🏗️   Building Turnstile app(s)..."

dotnet publish -c Release -o ./api_topublish ../Turnstile.Api/Turnstile.Api.csproj

[[ -z $p_headless ]] && dotnet publish -c Release -o ./web_topublish ../Turnstile.Web/Turnstile.Web.csproj

# Once the builds are finished, pack them up for deployment.

cd ./api_topublish
zip -r ../api_topublish.zip . >/dev/null
cd ..

if [[ -z $p_headless ]]; then

    cd ./web_topublish
    zip -r ../web_topublish.zip . >/dev/null
    cd ..

fi

echo "☁️    Publishing Turnstile app(s)..."

# We can deploy both apps in parallel and hopefully save a little time...

az functionapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$api_app_name" \
    --src "./api_topublish.zip" &

deploy_api_pid=$!

if [[ -z $p_headless ]]; then

    az webapp deployment source config-zip \
        --resource-group "$resource_group_name" \
        --name "$web_app_name" \
        --src "./web_topublish.zip" &

    deploy_web_pid=$!

    wait $deploy_web_pid

fi

wait $deploy_api_pid

echo "🧹   Cleaning up..."

rm -rf ./api_topublish >/dev/null
rm -rf ./api_topublish.zip >/dev/null

if [[ -z $p_headless ]]; then

    rm -rf ./web_topublish >/dev/null
    rm -rf ./web_topublish.zip >/dev/null

fi

echo "🏁   Turnstile deployment complete. It took [$SECONDS] seconds."
[[ -z $p_headless ]] && echo "➡️   Please go to [ $web_app_base_url/publisher/setup ] to complete setup."
echo






